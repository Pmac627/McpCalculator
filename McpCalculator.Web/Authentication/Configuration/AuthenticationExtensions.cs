using System.Diagnostics.CodeAnalysis;
using System.Text;
using McpCalculator.Web.Authentication.Configuration.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.IdentityModel.Tokens;

namespace McpCalculator.Web.Authentication.Configuration
{
    /// <summary>
    /// Extension methods for configuring authentication in the MCP Calculator web server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides a unified way to configure different authentication schemes
    /// based on the "Authentication:Type" setting in appsettings.json.
    /// </para>
    /// <para><b>Supported Authentication Types:</b></para>
    /// <list type="bullet">
    ///   <item><b>None:</b> No authentication (development only)</item>
    ///   <item><b>ApiKey:</b> Simple API key in custom header</item>
    ///   <item><b>Jwt:</b> JSON Web Token (Bearer) authentication</item>
    ///   <item><b>Windows:</b> Windows/NTLM authentication (IIS only)</item>
    /// </list>
    /// </remarks>
    [ExcludeFromCodeCoverage(Justification = "Configuration class")]
    public static class AuthenticationExtensions
    {
        /// <summary>
        /// The authentication scheme name used by this application.
        /// </summary>
        public const string SchemeName = "CalculatorAuth";

        /// <summary>
        /// Adds calculator authentication services based on configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// The authentication type is read from "Authentication:Type" in configuration.
        /// Each type has its own configuration section:
        /// </para>
        /// <list type="bullet">
        ///   <item><b>ApiKey:</b> Uses "Authentication:ApiKey" section</item>
        ///   <item><b>Jwt:</b> Uses "Authentication:Jwt" section</item>
        ///   <item><b>Windows:</b> No additional configuration needed</item>
        ///   <item><b>None:</b> No authentication middleware added</item>
        /// </list>
        /// </remarks>
        /// <example>
        /// In Program.cs:
        /// <code>
        /// var builder = WebApplication.CreateBuilder(args);
        /// builder.Services.AddCalculatorAuthentication(builder.Configuration);
        /// </code>
        /// </example>
        public static IServiceCollection AddCalculatorAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var authType = configuration.GetValue<string>("Authentication:Type") ?? "ApiKey";

            switch (authType.ToLowerInvariant())
            {
                case "none":
                    ConfigureNoAuthentication(services);
                    break;

                case "apikey":
                    ConfigureApiKeyAuthentication(services, configuration);
                    break;

                case "jwt":
                    ConfigureJwtAuthentication(services, configuration);
                    break;

                case "windows":
                    ConfigureWindowsAuthentication(services);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unknown authentication type: '{authType}'. " +
                        $"Valid values are: None, ApiKey, Jwt, Windows");
            }

            return services;
        }

        /// <summary>
        /// Configures no authentication (development/testing only).
        /// </summary>
        /// <remarks>
        /// <para><b>WARNING:</b> Do not use in production! This allows anonymous access
        /// to all endpoints.</para>
        /// <para>
        /// This is useful for local development and testing where authentication
        /// would add unnecessary friction.
        /// </para>
        /// </remarks>
        private static void ConfigureNoAuthentication(IServiceCollection services)
        {
            // Add a permissive authorization policy that allows anonymous access
            services.AddAuthorizationBuilder()
                .AddPolicy("default", policy => policy.RequireAssertion(_ => true));

            services.AddAuthentication()
                .AddScheme<ApiKeyAuthOptions, NoOpAuthHandler>("NoAuth", _ => { });

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = options.GetPolicy("default")!;
            });
        }

        /// <summary>
        /// Configures API Key authentication.
        /// </summary>
        /// <remarks>
        /// <para>
        /// API Key authentication validates a key sent in a custom HTTP header.
        /// This is simple to implement and use, making it suitable for internal
        /// services and simple integrations.
        /// </para>
        /// <para><b>Configuration:</b></para>
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
        /// </remarks>
        private static void ConfigureApiKeyAuthentication(
            IServiceCollection services,
            IConfiguration configuration)
        {
            var apiKeySection = configuration.GetSection("Authentication:ApiKey");

            services.AddAuthentication(SchemeName)
                .AddScheme<ApiKeyAuthOptions, ApiKeyAuthHandler>(SchemeName, options =>
                {
                    options.HeaderName = apiKeySection.GetValue<string>("HeaderName") ?? "X-API-Key";
                    options.ValidKeys = apiKeySection.GetSection("ValidKeys").Get<List<string>>() ?? new List<string>();
                });

            services.AddAuthorization();
        }

        /// <summary>
        /// Configures JWT Bearer authentication.
        /// </summary>
        /// <remarks>
        /// <para>
        /// JWT (JSON Web Token) authentication is a standard approach for web APIs.
        /// Clients obtain a token from an identity provider and include it in the
        /// Authorization header as a Bearer token.
        /// </para>
        /// <para><b>Configuration:</b></para>
        /// <code>
        /// {
        ///   "Authentication": {
        ///     "Type": "Jwt",
        ///     "Jwt": {
        ///       "Issuer": "McpCalculator",
        ///       "Audience": "McpCalculatorClients",
        ///       "SecretKey": "your-secret-key-min-32-chars"
        ///     }
        ///   }
        /// }
        /// </code>
        /// <para><b>Client Usage:</b></para>
        /// <code>
        /// Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
        /// </code>
        /// </remarks>
        private static void ConfigureJwtAuthentication(
            IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection("Authentication:Jwt");

            var issuer = jwtSection.GetValue<string>("Issuer") ?? "McpCalculator";
            var audience = jwtSection.GetValue<string>("Audience") ?? "McpCalculatorClients";
            var secretKey = jwtSection.GetValue<string>("SecretKey")
                ?? throw new InvalidOperationException("JWT SecretKey is required in configuration");

            if (secretKey.Length < 32)
            {
                throw new InvalidOperationException(
                    "JWT SecretKey must be at least 32 characters for security");
            }

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                        ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILogger<Program>>();

                            logger.LogWarning("JWT authentication failed: {Error}",
                                context.Exception.Message);

                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization();
        }

        /// <summary>
        /// Configures Windows (Negotiate/NTLM) authentication.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Windows authentication uses the Negotiate protocol (Kerberos/NTLM) for
        /// automatic authentication based on the user's Windows credentials.
        /// This is ideal for intranet applications hosted on IIS.
        /// </para>
        /// <para><b>Requirements:</b></para>
        /// <list type="bullet">
        ///   <item>Must be hosted on IIS or IIS Express</item>
        ///   <item>Windows Authentication must be enabled in IIS</item>
        ///   <item>Anonymous Authentication should be disabled</item>
        /// </list>
        /// <para>
        /// No additional configuration is needed in appsettings.json - authentication
        /// is handled automatically by IIS.
        /// </para>
        /// </remarks>
        private static void ConfigureWindowsAuthentication(IServiceCollection services)
        {
            services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                .AddNegotiate();

            services.AddAuthorization();
        }
    }
}