using Virtuademy.SDK.Core.SystemFramework;

using System;
using System.Threading.Tasks;

using UnityEngine.Events;

namespace Virtuademy.SDK.Core.Authentication
{
    /// <summary>
    /// The user-facing authentication surface: sign-in state, session loading, and — through
    /// <see cref="ITokenProvider"/> — the tokens API clients sign with.
    /// </summary>
    /// <remarks>
    /// <see cref="ITokenProvider.FindToken"/> and <see cref="ITokenProvider.GetTokens"/> used to
    /// be declared here. They moved to that interface so the transport can depend on the two
    /// operations it needs rather than on the whole authentication system; this interface still
    /// exposes them, so every existing caller is unaffected.
    /// </remarks>
    public interface IAuthenticationSystem : ISystem, ITokenProvider
    {
        [Flags]
        public enum EAuthentication
        {
            None = 0,
            Bearer = 1,
            Hmac = 2,
            BearerAndHmac = Bearer | Hmac
        }

        UnityEvent OnAuthenticated { get; }
        UnityEvent OnUnauthenticated { get; }
        UnityEvent<long, string> OnAuthenticationError { get; }

        Task ReloadSession(string sessionHash);
    }
}
