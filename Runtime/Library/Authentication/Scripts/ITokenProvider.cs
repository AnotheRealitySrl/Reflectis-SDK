using Virtuademy.SDK.Core.Utilities;

using System.Threading.Tasks;

namespace Virtuademy.SDK.Core.Authentication
{
    /// <summary>
    /// Supplies and refreshes the bearer tokens an API client signs its calls with.
    /// </summary>
    /// <remarks>
    /// Split out of <see cref="IAuthenticationSystem"/> so that the transport can ask for a
    /// token without depending on the thing that authenticates a user. The transport needs two
    /// operations; the authentication system has those plus session loading, sign-in events and
    /// a Unity lifecycle, none of which a client making an HTTP call has any business knowing
    /// about.
    /// <para>
    /// This is the seam the step-5 refactor turns into constructor injection: once
    /// <c>ApiSystemBase</c> is a plain instantiable client rather than a
    /// <c>ScriptableObject</c> system, the provider arrives as an argument instead of being
    /// looked up. <see cref="IAuthenticationSystem"/> derives from this interface, so the one
    /// implementation satisfies it already and nothing has to be rewired to adopt it.
    /// </para>
    /// </remarks>
    public interface ITokenProvider
    {
        /// <summary>
        /// The cached token for an API, identified by the label the API reports for itself in
        /// <c>GET /apiserver/info</c>. Throws when no token is held for that label.
        /// </summary>
        JwtToken FindToken(string apiLabel);

        /// <summary>
        /// Refreshes the whole token set. Called when a token is found expired, so the next
        /// <see cref="FindToken"/> returns a live one.
        /// </summary>
        Task GetTokens();
    }
}
