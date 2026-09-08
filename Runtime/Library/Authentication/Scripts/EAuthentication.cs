using System;

namespace Virtuademy.SDK.Core.Authentication
{
    /// <summary>
    /// Which credentials a request carries. HMAC and Bearer are independent header assignments,
    /// so the combination is a real option rather than a convenience.
    /// </summary>
    /// <remarks>
    /// This used to be nested inside <c>IAuthenticationSystem</c>, which meant every file
    /// that only wanted to name an auth mode had to import the authentication system — through a
    /// <c>using static</c> on an interface, at that. It is a property of a request, not of the
    /// system that signs one, and the nesting was the only thing tying the transport to an
    /// interface it otherwise has no use for.
    /// </remarks>
    [Flags]
    public enum EAuthentication
    {
        None = 0,
        Bearer = 1,
        Hmac = 2,
        BearerAndHmac = Bearer | Hmac
    }
}
