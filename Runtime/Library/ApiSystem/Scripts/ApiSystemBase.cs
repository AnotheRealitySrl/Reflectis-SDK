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
        /// <c>AI</c>, <c>Realtime</c>, …), used to resolve its base URL from endpoint
        /// discovery instead of from the build. See ADR 0024 in the meta-repo.
        /// </summary>
        /// <remarks>
        /// Null — the default — opts out and keeps the serialized
        /// <see cref="AppIdentification.ApiBaseUrl"/>. The system that performs discovery
        /// must stay opted out: it is the one endpoint that cannot be discovered, since
        /// it is the one being asked.
        /// </remarks>
        protected virtual string DiscoveryApiType => null;
        #endregion

        public override async Task Init()
        {
            if (string.IsNullOrEmpty(apiConfig.Credential.AppId.ToString()))
            {
                throw new Exception($"{name}: Missing {nameof(HmacCredential.AppId)}");
            }

            if (string.IsNullOrEmpty(apiConfig.Credential.AppSecret))
            {
                throw new Exception($"{name}: Missing {nameof(HmacCredential.AppSecret)}");
            }

            // Endpoint discovery (ADR 0024): prefer the base URL the platform reports for
            // this API type over the one serialized into the build, so moving an API to a
            // new hostname stops requiring a rebuild of the client.
            //
            // The serialized value is the fallback, and deliberately so: if no resolver is
            // registered yet — this system initialising before the bootstrap one, or the
            // platform unreachable — the system behaves exactly as it did before. That
            // makes the boot order a preference rather than a requirement.
            //
            // A serialized localhost address wins outright: it can only have been set by
            // someone deliberately pointing this build at an API on their own machine, and
            // the platform would otherwise answer with the deployed hostname and take it
            // away. Same rule the web clients apply to their own local override.
            if (!string.IsNullOrEmpty(DiscoveryApiType)
                && !PointsAtLocalhost(apiConfig.ApiBaseUrl)
                && ApiEndpointResolver.Current != null
                && ApiEndpointResolver.Current.TryGetBaseUrl(DiscoveryApiType, out string discoveredBaseUrl)
                && !string.IsNullOrEmpty(discoveredBaseUrl))
            {
                if (!string.Equals(discoveredBaseUrl, apiConfig.ApiBaseUrl, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"{name}: base URL resolved from discovery: {discoveredBaseUrl} " +
                              $"(the build carried {apiConfig.ApiBaseUrl})");
                }

                apiConfig = new AppIdentification(apiConfig.Credential, discoveredBaseUrl, apiConfig.ApiVersion);
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
