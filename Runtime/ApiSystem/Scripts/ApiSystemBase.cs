using Virtuademy.SDK.Core.Authentication;
using Virtuademy.SDK.Core.SystemFramework;
using Virtuademy.SDK.Core.Utilities;
using Virtuademy.SDK.Http;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

namespace Virtuademy.SDK.Core.ApiSystem
{
    /// <summary>
    /// A platform API client that is also a framework system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The client behaviour is <see cref="ApiClientBase"/>'s and lives there. This class holds
    /// only what a <c>ScriptableObject</c> adds to it — the serialized configuration, the asset
    /// name as a label, and the framework it can reach — and forwards the rest to an owned
    /// instance.
    /// </para>
    /// <para>
    /// <b>Why the forwarding is worth its awkwardness.</b> Until now this file and
    /// <c>ApiClientBase</c> were the same three hundred and eighty lines twice over, and they
    /// had already drifted: the token-provider fallback below was replaced by a throw in the
    /// copy, a difference in behaviour no reader of either file could see. One implementation
    /// cannot drift from itself.
    /// </para>
    /// <para>
    /// <b>How the virtual members still work.</b> The owned client is a private subclass that
    /// overrides each of them to call back into this system, so a subclass overriding
    /// <see cref="DiscoveryApiType"/> or <see cref="ValidateJwtToken"/> is obeyed by the
    /// client's own <c>Init</c> and <c>BuildRequest</c> exactly as before. The defaults here
    /// reach the real implementations through <c>base</c> calls the nested class exposes, so
    /// nothing recurses.
    /// </para>
    /// </remarks>
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

        private Client client;

        /// <summary>
        /// The client half of this system. Built on first use rather than in a constructor,
        /// which a <c>ScriptableObject</c> does not usefully have.
        /// </summary>
        private Client Api => client ??= new Client(this);
        #endregion

        #region Properties
        public AppIdentification ApiConfig { get => apiConfig; set => apiConfig = value; }

        public JwtToken JwtToken { get => Api.JwtToken; set => Api.JwtToken = value; }

        public TimeSpan ServerTimeOffset { get => serverTimeOffset; set => serverTimeOffset = value; }

        /// <summary>
        /// Where this client gets its bearer tokens. Left null — which it is everywhere today —
        /// <see cref="ValidateJwtToken"/> resolves the authentication system through the
        /// framework instead.
        /// </summary>
        /// <remarks>
        /// Settable rather than constructor-injected because this type is a
        /// <c>ScriptableObject</c>, which has no usable constructor. The framework fallback
        /// lives in this class and not in <see cref="ApiClientBase"/> on purpose: naming
        /// <c>SM</c> is the one thing a system can do and a plain client cannot.
        /// </remarks>
        public ITokenProvider Tokens { get; set; }

        public string ApiLabel => Api.ApiLabel;

        /// <summary>
        /// Canonical platform type of the API this system talks to (<c>Application</c>,
        /// <c>Profile</c>, <c>Realtime</c>, <c>Configuration</c>…) — the key its address is
        /// looked up by: the live resolver (ADR 0024) and the generated endpoint asset
        /// (ADR 0025).
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
        /// These are two different sources and only one of them is circular. The bootstrap
        /// system cannot ask the resolver — it <i>is</i> the resolver, and at the point it needs
        /// its own address the fetch that would answer has not happened yet. The generated asset
        /// has no such problem: it is a file on disk, written at tenant switch, so reading it is
        /// not a request.
        /// </remarks>
        protected virtual bool UseRuntimeResolver => true;
        #endregion

        public override Task Init() => Api.Init();

        /// <summary>
        /// Initialises with a configuration the caller supplies, overriding what this system
        /// carries — except for a loopback address, which is kept.
        /// </summary>
        public Task Init(AppIdentification config) => Api.Init(config);

        protected virtual Task<UnityWebRequest> BuildRequest(
                                                string method,
                                                string endpoint,
                                                Dictionary<string, string> queryParams = null,
                                                HttpHelper.ERequestBodyType requestBodyType = HttpHelper.ERequestBodyType.RawString,
                                                object body = null,
                                                EAuthentication authentication = EAuthentication.BearerAndHmac,
                                                bool allowEmptyQueryValues = false,
                                                Dictionary<string, string> additionalHeaders = null)
            => Api.Request(method, endpoint, queryParams, requestBodyType, body,
                           authentication, allowEmptyQueryValues, additionalHeaders);

        protected virtual Dictionary<string, string> SetDefaultHeaders(params string[] values)
            => Api.DefaultHeaders(values);

        protected virtual Task ValidateJwtToken()
        {
            // The framework lookup, and the reason this member is overridden at the system
            // layer at all: ApiClientBase cannot reach SM, and throws when handed no provider.
            Api.Tokens = Tokens ?? SM.GetSystem<IAuthenticationSystem>();

            return Api.ValidateToken();
        }

        public Task<bool> IsAlive() => Api.IsAlive();

        public void SetApiConfig(AppIdentification config) => apiConfig = config;

        /// <summary>
        /// The client, wired so that every virtual member resolves against the system rather
        /// than against itself.
        /// </summary>
        /// <remarks>
        /// <c>apiConfig</c> and <c>serverTimeOffset</c> are windows onto the system's own
        /// storage, not copies, and that matters for more than tidiness: one subclass assigns
        /// <c>apiConfig</c> <i>after</i> awaiting <c>base.Init()</c>, and against a copy that
        /// write would be invisible to every request the client went on to build.
        /// </remarks>
        private sealed class Client : ApiClientBase
        {
            private readonly ApiSystemBase owner;

            public Client(ApiSystemBase owner)
            {
                this.owner = owner;

                Label = owner.name;
                checkIsAlive = owner.checkIsAlive;
                getApiInfo = owner.getApiInfo;
                allowUntrustedServers = owner.allowUntrustedServers;
            }

            protected override AppIdentification apiConfig
            {
                get => owner.apiConfig;
                set => owner.apiConfig = value;
            }

            protected override TimeSpan serverTimeOffset
            {
                get => owner.serverTimeOffset;
                set => owner.serverTimeOffset = value;
            }

            protected override string DiscoveryApiType => owner.DiscoveryApiType;

            protected override bool UseRuntimeResolver => owner.UseRuntimeResolver;

            protected override Task ValidateJwtToken() => owner.ValidateJwtToken();

            protected override Dictionary<string, string> SetDefaultHeaders(params string[] values)
                => owner.SetDefaultHeaders(values);

            // The three below reach ApiClientBase's own implementations, which is what this
            // system's default overrides forward to. Without them the pair above would recurse.
            public Task ValidateToken() => base.ValidateJwtToken();

            public Dictionary<string, string> DefaultHeaders(params string[] values)
                => base.SetDefaultHeaders(values);

            public Task<UnityWebRequest> Request(string method,
                                                 string endpoint,
                                                 Dictionary<string, string> queryParams,
                                                 HttpHelper.ERequestBodyType requestBodyType,
                                                 object body,
                                                 EAuthentication authentication,
                                                 bool allowEmptyQueryValues,
                                                 Dictionary<string, string> additionalHeaders)
                => base.BuildRequest(method, endpoint, queryParams, requestBodyType, body,
                                     authentication, allowEmptyQueryValues, additionalHeaders);
        }
    }
}
