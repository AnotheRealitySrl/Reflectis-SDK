using UnityEngine;

namespace Virtuademy.SDK.Core.ApiSystem
{
    /// <summary>
    /// Reads the two generated configuration assets. One access path for the editor and for a
    /// player build, deliberately: a config the editor resolves differently from the build is a
    /// config that can be right in one and wrong in the other, and nothing would say so.
    /// See ADR 0025 in the meta-repo.
    /// </summary>
    /// <remarks>
    /// <b>Why <c>Resources</c> and not an inspector reference.</b> A <c>ScriptableObject</c> asset
    /// that nothing included in the build references is not in the player at all — it would resolve
    /// in the editor and be absent on device, which is the worst shape a configuration bug can
    /// take. <c>Resources</c> makes inclusion a property of where the asset sits rather than of
    /// somebody remembering to wire a field, and it is already this project's idiom for
    /// config-like assets (Photon's <c>ServerSettings</c> is loaded the same way).
    /// <para>
    /// Both loads are cached, including the negative result: a project that has never run the
    /// generator asks <c>Resources</c> once and then stops, instead of paying a failed load per
    /// system per boot.
    /// </para>
    /// </remarks>
    public static class PlatformConfig
    {
        /// <summary>
        /// Resource name, so the assets must live in a folder called <c>Resources</c> — anywhere in
        /// the project, which is what lets a creator keep them wherever they like.
        /// </summary>
        public const string EndpointsResourceName = "PlatformEndpoints";
        public const string CredentialsResourceName = "PlatformCredentials";

        private static PlatformEndpoints endpoints;
        private static bool endpointsLoaded;

        private static PlatformCredentials credentials;
        private static bool credentialsLoaded;

        /// <summary>
        /// The generated endpoint table, or null when the project has none. Null is a supported
        /// state: every consumer falls back to what it already had, which is the same degradation
        /// ADR 0024 defines for a resolver that has not answered.
        /// </summary>
        public static PlatformEndpoints Endpoints
        {
            get
            {
                if (!endpointsLoaded)
                {
                    endpoints = Resources.Load<PlatformEndpoints>(EndpointsResourceName);
                    endpointsLoaded = true;

                    if (endpoints == null)
                    {
                        Debug.Log($"[PlatformConfig] No {EndpointsResourceName} in any Resources folder. " +
                                  "APIs will use the base URL each one carries. Run the tenant switch to generate it.");
                    }
                }

                return endpoints;
            }
        }

        /// <summary>
        /// The generated credential, or null when the project has none.
        /// </summary>
        public static PlatformCredentials Credentials
        {
            get
            {
                if (!credentialsLoaded)
                {
                    credentials = Resources.Load<PlatformCredentials>(CredentialsResourceName);
                    credentialsLoaded = true;

                    if (credentials == null)
                    {
                        Debug.Log($"[PlatformConfig] No {CredentialsResourceName} in any Resources folder. " +
                                  "APIs will use the credential each one carries.");
                    }
                }

                return credentials;
            }
        }

        /// <summary>
        /// Convenience over <see cref="PlatformEndpoints.TryGet"/> that tolerates the asset being
        /// absent, so a caller needs one branch rather than two.
        /// </summary>
        public static bool TryGetEndpoint(string apiType, out PlatformEndpoint endpoint)
        {
            endpoint = null;
            return Endpoints != null && Endpoints.TryGet(apiType, out endpoint);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Drops the cache. The generator calls this after writing, so an editor session that has
        /// already read a stale asset — or read none at all before one existed — sees the new
        /// content without a domain reload.
        /// </summary>
        public static void InvalidateCache()
        {
            endpoints = null;
            endpointsLoaded = false;
            credentials = null;
            credentialsLoaded = false;
        }
#endif
    }
}
