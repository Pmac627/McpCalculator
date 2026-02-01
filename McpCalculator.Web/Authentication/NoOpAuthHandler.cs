using System.Security.Claims;
using System.Text.Encodings.Web;
using McpCalculator.Web.Authentication.Configuration.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace McpCalculator.Web.Authentication
{
    /// <summary>
    /// A no-op authentication handler that always succeeds.
    /// Used when authentication is disabled (Type: "None").
    /// </summary>
    /// <remarks>
    /// <para><b>WARNING:</b> This handler allows all requests without authentication.
    /// Only use for development and testing.</para>
    /// </remarks>
    public class NoOpAuthHandler : ApiKeyAuthHandler
    {
        /// <summary>
        /// Creates a new instance of the no-op authentication handler.
        /// </summary>
        public NoOpAuthHandler(
            IOptionsMonitor<ApiKeyAuthOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        /// <summary>
        /// Always returns success, allowing all requests.
        /// </summary>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "Anonymous"),
                new Claim(ClaimTypes.AuthenticationMethod, "None")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}