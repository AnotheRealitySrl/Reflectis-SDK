using Newtonsoft.Json;

using Virtuademy.SDK.Core.Authentication;
using Virtuademy.SDK.Core.SystemFramework;
using Virtuademy.SDK.Core.Utilities;
using Virtuademy.SDK.Http;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

using static Virtuademy.SDK.Core.Authentication.IAuthenticationSystem;

namespace Virtuademy.SDK.Core.ApiSystem
{
    public abstract class ApiSystemBase : BaseSystem
    {
        #region Inspector info
        [Header("General API Info")]
        [SerializeField] protected AppIdentification apiConfig;

        [Header("API Configuration")]
        [SerializeField] private bool checkIsAlive = true;
        [SerializeField] private bool getApiInfo = true;

        [Header("Untrusted servers")]
        [SerializeField] private bool allowUntrustedServers;
        #endregion

        #region Private info
        // Runtime state (not serialized, populated by the static API class)
        protected TimeSpan serverTimeOffset;
        #endregion

        #region Properties
        public AppIdentification ApiConfig { get => apiConfig; set => apiConfig = value; }

        public JwtToken JwtToken { get; set; }
        public TimeSpan ServerTimeOffset { get => serverTimeOffset; set => serverTimeOffset = value; }

        public string ApiLabel { get; private set; }

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

        public override async Task Init()
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
                    Debug.Log($"{name}: credential taken from the generated asset (app {generated.AppId})");
                }

                apiConfig = new AppIdentification(generated, apiConfig.ApiBaseUrl, apiConfig.ApiVersion);
            }

            if (apiConfig.Credential == null)
            {
                throw new Exception($"{name}: no credential — neither a generated {nameof(PlatformCredentials)} " +
                                    "asset nor one serialized on this system");
            }

            if (apiConfig.Credential.AppId == Guid.Empty)
            {
                throw new Exception($"{name}: Missing {nameof(HmacCredential.AppId)}");
            }

            if (string.IsNullOrEmpty(apiConfig.Credential.AppSecret))
            {
                throw new Exception($"{name}: Missing {nameof(HmacCredential.AppSecret)}");
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
                        Debug.Log($"{name}: base URL resolved from {resolvedFrom}: {resolvedBaseUrl} " +
                                  $"(this system carried {apiConfig.ApiBaseUrl})");
                    }

                    apiConfig = new AppIdentification(apiConfig.Credential,
                                                      resolvedBaseUrl,
                                                      resolvedVersion ?? apiConfig.ApiVersion);
                }
            }

            if (string.IsNullOrEmpty(apiConfig.ApiBaseUrl))
            {
                throw new Exception($"{name}: Missing {nameof(AppIdentification.ApiBaseUrl)}");
            }

            if (checkIsAlive)
            {
                if (!await ApiHelper.IsAlive(apiConfig, !allowUntrustedServers))
                {
                    throw new Exception($"{name}: API is not alive");
                }
            }

            if (getApiInfo)
            {
                ApiResponse<ApiInfo> apiInfoReq = await ApiHelper.GetApiInfo(apiConfig, !allowUntrustedServers);
                if (apiInfoReq.IsSuccess)
                {
                    ApiInfo apiInfo = apiInfoReq.Content;
                    Debug.Log($"{name}: API Server Info: {JsonConvert.SerializeObject(apiInfo)}");

                    ApiLabel = apiInfo.Label;
                    serverTimeOffset = DateTime.UtcNow - apiInfo.ServerTime;
                }
                else
                {
                    throw new Exception($"{name}: Failed to get API info: {apiInfoReq.StatusCode} {apiInfoReq.ReasonPhrase}");
                }
            }
        }

        public async Task Init(AppIdentification config)
        {
            apiConfig = config ?? throw new ArgumentException($"{this}: Missing AppConfig", nameof(AppIdentification));

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
            IAuthenticationSystem authenticationSystem = SM.GetSystem<IAuthenticationSystem>();

            if (JwtToken == null)
            {
                SetToken();
            }

            if (JwtToken.IsExpired(serverTimeOffset))
            {
                Debug.LogWarning($"[{name}]: JWT token is null or expired. Refreshing token for API label: {ApiLabel}");

                await authenticationSystem.GetTokens();

                SetToken();
            }

            void SetToken()
            {
                try
                {
                    JwtToken = authenticationSystem.FindToken(ApiLabel);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{name}]: Failed to retrieve JWT token for API label: {ApiLabel}. Exception: {ex.Message}");
                    return;
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
