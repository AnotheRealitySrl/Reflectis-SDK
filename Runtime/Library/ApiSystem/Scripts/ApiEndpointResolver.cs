namespace Virtuademy.SDK.Core.ApiSystem
{
    /// <summary>
    /// Resolves the base URL of an API from its canonical platform type
    /// (<c>Application</c>, <c>AI</c>, <c>Realtime</c>, …) instead of from the value
    /// serialized into the build. See ADR 0024 in the meta-repo.
    /// </summary>
    public interface IApiEndpointResolver
    {
        /// <summary>
        /// True when the platform reported a base URL for <paramref name="apiType"/>.
        /// False leaves the caller on whatever it already had.
        /// </summary>
        bool TryGetBaseUrl(string apiType, out string baseUrl);
    }

    /// <summary>
    /// Registration point for the resolver.
    /// </summary>
    /// <remarks>
    /// A static hand-off rather than a direct reference because the system that performs
    /// discovery lives in <c>Virtuademy-SDK-TenantConfiguration</c>, which already depends
    /// on this assembly: referencing it back would be circular. The bootstrap system
    /// registers itself here once its fetch lands, and <see cref="ApiSystemBase"/> reads
    /// it without knowing who provided it.
    /// <para>
    /// <see cref="Current"/> is null until then, and staying null is a supported state:
    /// every consumer falls back to the base URL serialized in its own configuration, so
    /// a system that initialises before discovery — or a build where discovery never
    /// answers — behaves exactly as it did before.
    /// </para>
    /// </remarks>
    public static class ApiEndpointResolver
    {
        public static IApiEndpointResolver Current { get; set; }
    }
}
