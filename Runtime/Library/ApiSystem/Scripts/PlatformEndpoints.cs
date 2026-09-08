using System;
using System.Collections.Generic;

using UnityEngine;

namespace Virtuademy.SDK.Core.ApiSystem
{
    /// <summary>
    /// One API's address as the platform reported it, keyed on the canonical API type
    /// (<c>Application</c>, <c>AI</c>, <c>Realtime</c>, …) rather than on its label, because
    /// labels are tenant-scoped and change when a deployment is rebranded.
    /// </summary>
    [Serializable]
    public class PlatformEndpoint
    {
        [SerializeField] private string apiType;
        [SerializeField] private string baseUrl;
        [SerializeField] private string apiVersion;

        public string ApiType => apiType;
        public string BaseUrl => baseUrl;
        public string ApiVersion => apiVersion;

        public PlatformEndpoint() { }

        public PlatformEndpoint(string apiType, string baseUrl, string apiVersion)
        {
            this.apiType = apiType;
            this.baseUrl = baseUrl;
            this.apiVersion = apiVersion;
        }
    }

    /// <summary>
    /// The endpoints this build talks to, written by the editor tooling at tenant switch from
    /// <c>GET /manage/apps/api-endpoints</c>. See ADR 0025 in the meta-repo.
    /// <para>
    /// This replaces a base URL and version serialized separately into each API system's own
    /// asset — 23 of them, per platform variant, each a copy that could drift from the others.
    /// One asset, one source, and a diff that shows when an environment moves.
    /// </para>
    /// <para>
    /// <b>It carries no secret and is meant to be committed.</b> The credential lives in
    /// <see cref="PlatformCredentials"/>, which is generated and gitignored, precisely so that
    /// git can tell a hostname change from a credential rotation. Do not add credential fields
    /// here.
    /// </para>
    /// <para>
    /// <b>It is the build-time baseline, not the runtime authority.</b> ADR 0024's
    /// <see cref="ApiEndpointResolver"/> still overrides it from the platform at runtime, so
    /// moving an API to a new hostname does not require a rebuild. What this asset removes is the
    /// need for the *fallback* to be hand-maintained in 23 places.
    /// </para>
    /// <para>
    /// No <c>[CreateAssetMenu]</c> on purpose: the asset is generated, and one created by hand
    /// would be empty and would shadow the generated one in a project-wide search.
    /// </para>
    /// </summary>
    public class PlatformEndpoints : ScriptableObject
    {
        [SerializeField] private string generatedFrom;
        [SerializeField] private string generatedAtUtc;
        [SerializeField] private List<PlatformEndpoint> endpoints = new();

        /// <summary>Which app and environment this was generated from, for review in a diff.</summary>
        public string GeneratedFrom => generatedFrom;

        /// <summary>When, in UTC round-trip format. Stale content is a question a reader can ask.</summary>
        public string GeneratedAtUtc => generatedAtUtc;

        public IReadOnlyList<PlatformEndpoint> Endpoints => endpoints;

        /// <summary>
        /// The endpoint recorded for <paramref name="apiType"/>. Matched case-insensitively, the
        /// same way the runtime resolver matches, so the two cannot disagree over casing.
        /// </summary>
        public bool TryGet(string apiType, out PlatformEndpoint endpoint)
        {
            endpoint = null;

            if (string.IsNullOrEmpty(apiType) || endpoints == null)
            {
                return false;
            }

            foreach (PlatformEndpoint candidate in endpoints)
            {
                if (string.Equals(candidate?.ApiType, apiType, StringComparison.OrdinalIgnoreCase))
                {
                    endpoint = candidate;
                    return !string.IsNullOrEmpty(candidate.BaseUrl);
                }
            }

            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only writer, called by the generator at tenant switch. The runtime surface stays
        /// read-only: this asset is data the platform produced, and a consumer that could rewrite
        /// it would make "one source" a claim rather than a property.
        /// </summary>
        /// <remarks>
        /// Refuses an empty set. A failed fetch must leave the last known-good content in place —
        /// wiping it would turn one bad request into a build that points nowhere, and the caller
        /// cannot always tell the difference between "no endpoints" and "could not ask".
        /// </remarks>
        public bool Write(string generatedFrom, IEnumerable<PlatformEndpoint> entries)
        {
            List<PlatformEndpoint> incoming = new();
            if (entries != null)
            {
                foreach (PlatformEndpoint e in entries)
                {
                    if (e != null && !string.IsNullOrEmpty(e.ApiType) && !string.IsNullOrEmpty(e.BaseUrl))
                    {
                        incoming.Add(e);
                    }
                }
            }

            if (incoming.Count == 0)
            {
                return false;
            }

            this.generatedFrom = generatedFrom;
            generatedAtUtc = DateTime.UtcNow.ToString("O");
            endpoints = incoming;
            return true;
        }
#endif
    }
}
