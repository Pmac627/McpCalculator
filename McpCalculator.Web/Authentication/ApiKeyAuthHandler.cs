using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using McpCalculator.Web.Authentication.Configuration.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace McpCalculator.Web.Authentication
{
    /// <summary>
    /// Authentication handler that validates API keys from request headers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handler implements a simple API key authentication scheme.
    /// It checks for a specific header in the request and validates the
    /// value against a configured list of valid keys.
    /// </para>
    /// <para><b>Security Considerations:</b></para>
    /// <list type="bullet">
    ///   <item>Always use HTTPS to prevent key interception</item>
    ///   <item>Rotate keys periodically</item>
    ///   <item>Use different keys for different clients/environments</item>
    ///   <item>Consider rate limiting per API key</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// Client request with API key:
    /// <code>
    /// curl -X POST https://localhost:5001/mcp \
    ///      -H "X-API-Key: your-api-key-here" \
    ///      -H "Content-Type: application/json" \
    ///      -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'
    /// </code>
    /// </example>
    public class ApiKeyAuthHandler : AuthenticationHandler<ApiKeyAuthOptions>
    {
        /// <summary>
        /// Creates a new instance of the API key authentication handler.
        /// </summary>
        public ApiKeyAuthHandler(
            IOptionsMonitor<ApiKeyAuthOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        /// <summary>
        /// Handles the authentication challenge by validating the API key header.
        /// </summary>
        /// <returns>
        /// <see cref="AuthenticateResult.Success"/> if a valid key is provided;
        /// <see cref="AuthenticateResult.Fail(Exception)"/> otherwise.
        /// </returns>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Check if the API key header exists
            if (!Request.Headers.TryGetValue(Options.HeaderName, out var apiKeyHeaderValues))
            {
                return Task.FromResult(AuthenticateResult.Fail($"Missing {Options.HeaderName} header"));
            }

            var providedApiKey = apiKeyHeaderValues.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(providedApiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("API key is empty"));
            }

            // Validate the API key against the configured valid keys
            // Using constant-time comparison to prevent timing attacks
            var isValidKey = Options.ValidKeys.Any(validKey =>
                CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(providedApiKey),
                    System.Text.Encoding.UTF8.GetBytes(validKey)));

            if (!isValidKey)
            {
                Logger.LogWarning("Invalid API key attempted: {KeyPrefix}...",
                    providedApiKey.Length > 4 ? providedApiKey[..4] : "****");
                return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
            }

            // Create claims for the authenticated client
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "ApiKeyClient"),
                new Claim(ClaimTypes.AuthenticationMethod, "ApiKey"),
                new Claim("ApiKeyPrefix", providedApiKey.Length > 4 ? providedApiKey[..4] : "****")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            Logger.LogDebug("API key authentication successful");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        /// <summary>
        /// Handles the authentication challenge response (401 Unauthorized).
        /// </summary>
        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.Headers.Append("WWW-Authenticate", $"ApiKey realm=\"McpCalculator\", header=\"{Options.HeaderName}\"");
            return Task.CompletedTask;
        }
    }
}