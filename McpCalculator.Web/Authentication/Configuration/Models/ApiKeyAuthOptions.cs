using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authentication;

namespace McpCalculator.Web.Authentication.Configuration.Models
{
    /// <summary>
    /// Configuration options for API Key authentication.
    /// </summary>
    /// <remarks>
    /// API Key authentication is the simplest form of authentication.
    /// The client sends a key in a custom header, and the server validates
    /// it against a list of known valid keys.
    /// </remarks>
    /// <example>
    /// Configuration in appsettings.json:
    /// <code>
    /// {
    ///   "Authentication": {
    ///     "Type": "ApiKey",
    ///     "ApiKey": {
    ///       "HeaderName": "X-API-Key",
    ///       "ValidKeys": ["key1", "key2"]
    ///     }
    ///   }
    /// }
    /// </code>
    /// </example>
    [ExcludeFromCodeCoverage(Justification = "Model class")]
    public class ApiKeyAuthOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// The HTTP header name to look for the API key.
        /// </summary>
        /// <remarks>Default is "X-API-Key".</remarks>
        public string HeaderName { get; set; } = "X-API-Key";

        /// <summary>
        /// List of valid API keys that will be accepted.
        /// </summary>
        /// <remarks>
        /// In production, consider storing these securely (e.g., Azure Key Vault)
        /// rather than in configuration files.
        /// </remarks>
        public List<string> ValidKeys { get; set; } = new();
    }
}