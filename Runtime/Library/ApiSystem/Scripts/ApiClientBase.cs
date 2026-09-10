using Newtonsoft.Json;

using Virtuademy.SDK.Core.Authentication;
using Virtuademy.SDK.Core.Utilities;
using Virtuademy.SDK.Http;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

namespace Virtuademy.SDK.Core.ApiSystem
{
    public class ApiClientBase
    {
        public ApiClientBase() => Label = GetType().Name;

        #region Inspector info
        protected AppIdentification apiConfig;

        private bool checkIsAlive = true;
        private bool getApiInfo = true;

        private bool allowUntrustedServers;
        #endregion

        #region Private info
        // Runtime state (not serialized, populated by the static API class)
        protected TimeSpan serverTimeOffset;
        #endregion

        #region Properties
        public AppIdentification ApiConfig { get => apiConfig; set => apiConfig = value; }

        public JwtToken JwtToken { get; set; }
        public TimeSpan ServerTimeOffset { get => serverTimeOffset; set => serverTimeOffset = value; }

        /// <summary>
        /// Where this client gets its bearer tokens. Left null it falls back to resolving the
        /// authentication system through the framework, which is what every caller relies on
        /// today.
        /// </summary>
        /// <remarks>
        /// Settable rather than constructor-injected because this type is still a
        /// <c>ScriptableObject</c>, which has no usable constructor. That is the only reason —
        /// the parameter it becomes is the point of the refactor, and this property exists so
        /// the call site in <see cref="ValidateJwtToken"/> already reads the way it will read
        /// afterwards.
        /// </remarks>
        public ITokenProvider Tokens { get; set; }

        public string ApiLabel { get; private set; }

        /// <summary>
        /// What this client calls itself in logs and exceptions. It was the ScriptableObject's
        /// asset name; a plain object has none, so the owner supplies one and the type name is
        /// the default.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// True when the address points at the machine running this build, in which case
        /// endpoint discovery must not replace it — see <see cref="Init"/>.
        /// </summary>
        private static bool PointsAtLocalhost(string url)
        {
            if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out Uri parsed))
            {
                return false;
            }

            return parsed.IsLoopback;
        }

        /// <summary>
        /// Canonical platform type of the API this system talks to (<c>Application</c>,
        /// <c>AI</c>, <c>Realtime</c>, …). The key both platform-resolved sources are looked
        /// up by: the live resolver (ADR 0024) and the generated endpoint asset (ADR 0025).
        /// </summary>
        /// <remarks>
        /// Null — the default — opts out of both and keeps the serialized
        /// <see cref="AppIdentification.ApiBaseUrl"/>. To opt out of the live resolver alone,
        /// give a type here and override <see cref="UseRuntimeResolver"/>.
        /// </remarks>
        protected virtual string DiscoveryApiType => null;

        /// <summary>
        /// Whether this system may ask the live resolver, as opposed to reading only the
        /// generated asset. False for the system that performs discovery itself.
        /// </summary>
        /// <remarks>
        /// These are two different sources and only one of them is circular, which is the
        /// whole reason this is a separate switch rather than a second meaning of
        /// <see cref="DiscoveryApiType"/>. The bootstrap system cannot ask the resolver —
        /// it *is* the resolver, and at the point it needs its own address the fetch that
        /// would answer has not happened yet. The generated asset has no such problem: it
        /// is a file on disk, written at tenant switch, so reading it is not a request.
        /// <para>
        /// Conflating the two is what left the bootstrap system as the only one still
        /// pinned to a URL serialized into the build — and therefore the only one that
        /// broke outright when that field was blanked, since nothing could refill it.
        /// </para>
        /// </remarks>
        protected virtual bool UseRuntimeResolver => true;
        #endregion

        public virtual async Task Init()
        {
            // Credential: the generated asset first, then whatever this system carries (ADR 0025).
            //
            // Asset-first rather than the other way round, and the order is the whole point.
            // Preferring the serialized value would leave every system on the credential the old
            // tenant-switch stamping wrote into its own asset — so the migration would look done
            // while nothing had actually moved, and the 18 committed copies would stay live.
            if (PlatformConfig.Credentials != null && PlatformConfig.Credentials.HasCredential)
            {
                HmacCredential generated = PlatformConfig.Credentials.Credential;

                if (apiConfig.Credential == null || apiConfig.Credential.AppId != generated.AppId)
                {
                    Debug.Log($"{Label}: credential taken from the generated asset (app {generated.AppId})");
                }

                apiConfig = new AppIdentification(generated, apiConfig.ApiBaseUrl, apiConfig.ApiVersion);
            }

            if (apiConfig.Credential == null)
            {
                throw new Exception($"{Label}: no credential — neither a generated {nameof(PlatformCredentials)} " +
                                    "asset nor one serialized on this system");
            }

            if (apiConfig.Credential.AppId == Guid.Empty)
            {
                throw new Exception($"{Label}: Missing {nameof(HmacCredential.AppId)}");
            }

            if (string.IsNullOrEmpty(apiConfig.Credential.AppSecret))
            {
                throw new Exception($"{Label}: Missing {nameof(HmacCredential.AppSecret)}");
            }

            // Where this system's base URL comes from. Three sources, in this order, each a
            // fallback for the one above:
            //
            //   1. the runtime resolver — the platform, asked live, so a hostname can move
            //      without a rebuild of the client (ADR 0024). Skipped when UseRuntimeResolver
            //      is false, which is how the system that performs discovery avoids asking
            //      itself for an answer it does not have yet.
            //   2. the generated endpoint asset — what the platform said at the last tenant
            //      switch: one asset for the whole project instead of a copy serialized into
            //      every system (ADR 0025). Read through PlatformConfig, which goes via
            //      Resources so the editor and a player build resolve it identically.
            //   3. the base URL serialized in this system's own asset — the legacy source, and
            //      the only one that survives a project that has never run a tenant switch.
            //
            // Falling through all three to the serialized value is a supported outcome, not a
            // failure: a system that initialises before the bootstrap one, or a build whose
            // platform is unreachable, behaves exactly as it did before discovery existed.
            // That is what makes boot order a preference rather than a requirement.
            //
            // A serialized loopback address wins outright over all three. It can only have been
            // set by someone deliberately aiming this build at a service on their own machine,
            // and both the platform and the generated asset would otherwise answer with the
            // deployed hostname and quietly take it away. Same rule the web clients apply to
            // their own local override.
            if (!string.IsNullOrEmpty(DiscoveryApiType) && !PointsAtLocalhost(apiConfig.ApiBaseUrl))
            {
                string resolvedBaseUrl = null;
                string resolvedVersion = null;
                string resolvedFrom = null;

                if (UseRuntimeResolver
                    && ApiEndpointResolver.Current != null
                    && ApiEndpointResolver.Current.TryGetBaseUrl(DiscoveryApiType, out string discoveredBaseUrl)
                    && !string.IsNullOrEmpty(discoveredBaseUrl))
                {
                    resolvedBaseUrl = discoveredBaseUrl;
                    resolvedFrom = "the platform";
                }
                else if (PlatformConfig.TryGetEndpoint(DiscoveryApiType, out PlatformEndpoint generated))
                {
                    resolvedBaseUrl = generated.BaseUrl;
                    // Only when the asset has one: a type outside the four TenantConfig carries
                    // arrives without a version, and overwriting a good serialized value with
                    // nothing would be a regression.
                    resolvedVersion = string.IsNullOrEmpty(generated.ApiVersion) ? null : generated.ApiVersion;
                    resolvedFrom = $"the generated asset ({PlatformConfig.Endpoints.GeneratedFrom})";
                }

                if (!string.IsNullOrEmpty(resolvedBaseUrl))
                {
                    if (!string.Equals(resolvedBaseUrl, apiConfig.ApiBaseUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"{Label}: base URL resolved from {resolvedFrom}: {resolvedBaseUrl} " +
                                  $"(this system carried {apiConfig.ApiBaseUrl})");
                    }

                    apiConfig = new AppIdentification(apiConfig.Credential,
                                                      resolvedBaseUrl,
                                                      resolvedVersion ?? apiConfig.ApiVersion);
                }
            }

            if (string.IsNullOrEmpty(apiConfig.ApiBaseUrl))
            {
                throw new Exception($"{Label}: Missing {nameof(AppIdentification.ApiBaseUrl)}");
            }

            if (checkIsAlive)
            {
                if (!await ApiHelper.IsAlive(apiConfig, !allowUntrustedServers))
                {
                    throw new Exception($"{Label}: API is not alive");
                }
            }

            if (getApiInfo)
            {
                ApiResponse<ApiInfo> apiInfoReq = await ApiHelper.GetApiInfo(apiConfig, !allowUntrustedServers);
                if (apiInfoReq.IsSuccess)
                {
                    ApiInfo apiInfo = apiInfoReq.Content;
                    Debug.Log($"{Label}: API Server Info: {JsonConvert.SerializeObject(apiInfo)}");

                    ApiLabel = apiInfo.Label;
                    serverTimeOffset = DateTime.UtcNow - apiInfo.ServerTime;
                }
                else
                {
                    throw new Exception($"{Label}: Failed to get API info: {apiInfoReq.StatusCode} {apiInfoReq.ReasonPhrase}");
                }
            }
        }

        /// <summary>
        /// Initialises with a configuration the caller supplies, overriding what this system
        /// carries — except for a loopback address, which is kept.
        /// </summary>
        /// <remarks>
        /// <b>Why the loopback exception is here and not only in <see cref="Init()"/>.</b> The app
        /// stamps all four API systems through this overload at boot, from the live tenant config.
        /// Assigning unconditionally meant a developer's local override was written into the asset,
        /// read by nothing, and replaced with the deployed hostname before resolution even started
        /// — so the switcher that writes those addresses, and the loopback rule the resolution
        /// documents, were both inert for every system the app initialises. The address survived
        /// only for the bootstrap system, which is initialised without a configuration and was
        /// therefore the one case anybody had tested.
        /// <para>
        /// The credential still comes from the caller: pointing at a service on this machine is a
        /// statement about *where*, not about *who*.
        /// </para>
        /// </remarks>
        public async Task Init(AppIdentification config)
        {
            if (config == null)
            {
                throw new ArgumentException($"{this}: Missing AppConfig", nameof(AppIdentification));
            }

            if (PointsAtLocalhost(apiConfig?.ApiBaseUrl))
            {
                Debug.Log($"{Label}: keeping the local address {apiConfig.ApiBaseUrl} instead of the " +
                          $"supplied {config.ApiBaseUrl} — a loopback address is a deliberate override.");

                apiConfig = new AppIdentification(config.Credential, apiConfig.ApiBaseUrl, apiConfig.ApiVersion);
            }
            else
            {
                apiConfig = config;
            }

            await Init();
        }

        protected virtual async Task<UnityWebRequest> BuildRequest(
                                                string method,
                                                string endpoint,
                                                Dictionary<string, string> queryParams = null,
                                                HttpHelper.ERequestBodyType requestBodyType = HttpHelper.ERequestBodyType.RawString,
                                                object body = null,
                                                EAuthentication authentication = EAuthentication.BearerAndHmac,
                                                bool allowEmptyQueryValues = false,
                                                Dictionary<string, string> additionalHeaders = null)
        {
            if (authentication.HasFlag(EAuthentication.Bearer))
            {
                await ValidateJwtToken();
            }

            return ApiHelper.BuildRequest(
                method, endpoint, apiConfig,
                queryParams,
                requestBodyType,
                body,
                authentication,
                allowEmptyQueryValues,
                additionalHeaders,
                jwtToken: JwtToken,
                serverTimeOffset: serverTimeOffset,
                allowUntrustedServers: allowUntrustedServers);
        }

        protected virtual Dictionary<string, string> SetDefaultHeaders(params string[] values)
        {
            Dictionary<string, string> headers = new()
            {
                { "AppId", apiConfig.Credential.AppId.ToString() },
                { "Timestamp", values[0] },
            };

            return headers;
        }

        protected virtual async Task ValidateJwtToken()
        {
            // Injected provider first, the framework lookup as the fallback.
            //
            // This is the only ambient dependency in this class, and the property is the seam
            // that removes it: when this type becomes a plain instantiable client the provider
            // arrives as a constructor argument and the fallback below goes with it, leaving
            // the class declaration as the last thing here that names the framework at all.
            // Preferring the injected one now means that switch changes nothing about how this
            // method behaves.
            ITokenProvider tokenProvider = Tokens
                ?? throw new InvalidOperationException(
                    $"{Label}: no token provider. The owner sets Tokens before Init; "
                    + "a ScriptableObject system does it from the framework, a plain host "
                    + "passes one in.");

            if (JwtToken == null)
            {
                TrySetToken();
            }

            // A missing token and an expired one need the same thing, so they take the same
            // branch. Testing only for expiry used to dereference a null token: the first call
            // of a session reaches here with nothing cached, TrySetToken swallowed the
            // "no tokens available" it got back, and the very next line asked that null whether
            // it had expired. The logged error was therefore followed by a NullReference that
            // hid it.
            if (JwtToken == null || JwtToken.IsExpired(serverTimeOffset))
            {
                Debug.LogWarning($"[{Label}]: JWT token is missing or expired. Refreshing token for API label: {ApiLabel}");

                await tokenProvider.GetTokens();

                TrySetToken();
            }

            if (JwtToken == null)
            {
                Debug.LogError($"[{Label}]: no token for API label '{ApiLabel}' even after a refresh. " +
                               "The request will be sent without a bearer header and will most " +
                               "likely come back 401.");
            }

            // Reports rather than throws, which is the behaviour the original catch intended:
            // a caller that cannot get a token still sends its request and takes the 401. The
            // difference is that the reason now survives to the log instead of being buried by
            // the crash on the line after.
            void TrySetToken()
            {
                try
                {
                    JwtToken = tokenProvider.FindToken(ApiLabel);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[{Label}]: no JWT token held for API label '{ApiLabel}' yet: {ex.Message}");
                }
            }
        }

        public async Task<bool> IsAlive()
        {
            return await ApiHelper.IsAlive(apiConfig, !allowUntrustedServers);
        }

        public void SetApiConfig(AppIdentification config)
        {
            apiConfig = config;
        }
    }
}
