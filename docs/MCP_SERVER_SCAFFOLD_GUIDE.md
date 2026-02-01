# MCP Server Scaffold Guide for .NET

This guide provides step-by-step instructions for creating a production-ready MCP (Model Context Protocol) server in .NET with:
- Multi-project architecture (Core library + Console + Web)
- Dual transport support (stdio for local, HTTP/SSE for remote)
- Authentication (None, ApiKey, JWT, Windows)
- REST API endpoints alongside MCP
- Comprehensive testing with 100% code coverage

## Prerequisites

Before starting, gather the following information from the user:

1. **Server Name** (PascalCase, e.g., "WeatherService", "DatabaseManager", "FileProcessor")
2. **Server Purpose** (Brief description of what the server does)
3. **Tool Operations** (List of operations/tools the server will provide)
4. **Resource Limits** (e.g., max file size, max computation value, timeout duration)
5. **Rate Limiting** (e.g., 100 requests per minute, 1000 requests per hour)
6. **Transport Requirements**:
   - Stdio only (for Claude Desktop integration)
   - HTTP only (for remote/cloud deployment)
   - Both (recommended for flexibility)
7. **Authentication Requirements** (for HTTP transport):
   - None (development only)
   - API Key (simple header-based)
   - JWT (token-based with expiration)
   - Windows (NTLM/Kerberos for IIS)

## Project Structure

Create the following multi-project structure:

```
<ServerName>/
├── <ServerName>/                          # Console app (stdio transport)
│   ├── <ServerName>.csproj
│   └── Program.cs
├── <ServerName>.Core/                     # Shared business logic library
│   ├── <ServerName>.Core.csproj
│   ├── <ServerName>Tools.cs               # MCP tool implementation
│   ├── ResourceLimits.cs                  # Resource validation
│   ├── RateLimiter.cs                     # Rate limiting
│   └── ExecutionContext.cs                # Timeout enforcement
├── <ServerName>.Web/                      # Web app (HTTP transport)
│   ├── <ServerName>.Web.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Authentication/
│   │   ├── ApiKeyAuthHandler.cs
│   │   ├── NoOpAuthHandler.cs
│   │   ├── AuthenticationExtensions.cs
│   │   └── Configuration/
│   │       └── ApiKeyAuthOptions.cs
│   └── Api/
│       ├── <ServerName>ApiEndpoints.cs
│       └── Models/
│           ├── <ServerName>Request.cs
│           ├── <ServerName>Response.cs
│           └── <ServerName>ErrorResponse.cs
├── <ServerName>.Tests/                    # Test project
│   ├── <ServerName>.Tests.csproj
│   ├── coverlet.runsettings
│   ├── README.md
│   └── [Test files...]
├── docs/                                  # Documentation
│   ├── AUTHENTICATION.md
│   ├── REST_API.md
│   ├── HTTP_TRANSPORT.md
│   └── IIS_DEPLOYMENT.md
├── <ServerName>.sln                       # Solution file
└── README.md
```

## Step 1: Create Solution and Projects

### 1.1 Create Solution

```bash
mkdir <ServerName>
cd <ServerName>
dotnet new sln -n <ServerName>
```

### 1.2 Create Core Library

```bash
dotnet new classlib -n <ServerName>.Core
dotnet sln add <ServerName>.Core
cd <ServerName>.Core
dotnet add package ModelContextProtocol --version 0.6.0-preview.1
cd ..
```

### 1.3 Create Console App (Stdio Transport)

```bash
dotnet new console -n <ServerName>
dotnet sln add <ServerName>
cd <ServerName>
dotnet add package Microsoft.Extensions.Hosting --version 10.0.2
dotnet add reference ../<ServerName>.Core/<ServerName>.Core.csproj
cd ..
```

### 1.4 Create Web App (HTTP Transport)

```bash
dotnet new web -n <ServerName>.Web
dotnet sln add <ServerName>.Web
cd <ServerName>.Web
dotnet add package ModelContextProtocol.AspNetCore --version 0.3.0-preview.3
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.0
dotnet add package Microsoft.AspNetCore.Authentication.Negotiate --version 10.0.0
dotnet add reference ../<ServerName>.Core/<ServerName>.Core.csproj
cd ..
```

### 1.5 Create Test Project

```bash
dotnet new xunit -n <ServerName>.Tests
dotnet sln add <ServerName>.Tests
cd <ServerName>.Tests
dotnet add reference ../<ServerName>.Core/<ServerName>.Core.csproj
dotnet add reference ../<ServerName>.Web/<ServerName>.Web.csproj
dotnet add package Moq --version 4.20.72
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 10.0.0
cd ..
```

## Step 2: Configure Project Files

### 2.1 <ServerName>.Core.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="ModelContextProtocol" Version="0.6.0-preview.1" />
  </ItemGroup>

</Project>
```

### 2.2 <ServerName>.csproj (Console)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\<ServerName>.Core\<ServerName>.Core.csproj" />
  </ItemGroup>

</Project>
```

### 2.3 <ServerName>.Web.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <!-- Expose internal types to test project -->
  <ItemGroup>
    <InternalsVisibleTo Include="<ServerName>.Tests" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="ModelContextProtocol.AspNetCore" Version="0.3.0-preview.3" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.Negotiate" Version="10.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\<ServerName>.Core\<ServerName>.Core.csproj" />
  </ItemGroup>

</Project>
```

### 2.4 <ServerName>.Tests.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\<ServerName>.Core\<ServerName>.Core.csproj" />
    <ProjectReference Include="..\<ServerName>.Web\<ServerName>.Web.csproj" />
  </ItemGroup>

</Project>
```

## Step 3: Create Core Library Files

### 3.1 ResourceLimits.cs

```csharp
namespace <ServerName>.Core
{
    /// <summary>
    /// Defines resource limits for operations.
    /// Prevents resource exhaustion, overflow errors, and excessive computation.
    /// </summary>
    public static class ResourceLimits
    {
        /// <summary>
        /// Maximum absolute value allowed for numeric inputs.
        /// </summary>
        public const double MaxAbsoluteValue = 1e15;

        /// <summary>
        /// Minimum allowed denominator to prevent division by near-zero.
        /// </summary>
        public const double MinDenominator = 1e-10;

        /// <summary>
        /// Maximum execution time for any single operation.
        /// </summary>
        public static readonly TimeSpan MaxOperationTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Validates that a numeric value is within acceptable limits.
        /// </summary>
        public static void ValidateValueRange(double value, string paramName)
        {
            if (double.IsNaN(value))
            {
                throw new ArgumentException(
                    "Value cannot be NaN (Not a Number). Provide a valid numeric value.",
                    paramName);
            }

            if (double.IsInfinity(value))
            {
                throw new ArgumentException(
                    "Value cannot be Infinity. Provide a finite numeric value.",
                    paramName);
            }

            if (Math.Abs(value) > MaxAbsoluteValue)
            {
                throw new ArgumentOutOfRangeException(
                    paramName,
                    value,
                    $"Value exceeds maximum allowed magnitude of {MaxAbsoluteValue:E}.");
            }
        }

        /// <summary>
        /// Validates that a denominator is safe for division.
        /// </summary>
        public static void ValidateDenominator(double value, string paramName)
        {
            ValidateValueRange(value, paramName);

            if (Math.Abs(value) < MinDenominator)
            {
                throw new ArgumentException(
                    $"Denominator too close to zero (minimum: {MinDenominator:E}).",
                    paramName);
            }
        }

        /// <summary>
        /// Validates that a result is within acceptable limits.
        /// </summary>
        public static void ValidateResult(double result, string operationName)
        {
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                throw new InvalidOperationException(
                    $"{operationName} produced invalid result (NaN or Infinity).");
            }

            if (Math.Abs(result) > MaxAbsoluteValue)
            {
                throw new InvalidOperationException(
                    $"{operationName} exceeded maximum allowed magnitude of {MaxAbsoluteValue:E}.");
            }
        }
    }
}
```

### 3.2 RateLimiter.cs

```csharp
using System.Collections.Concurrent;

namespace <ServerName>.Core
{
    /// <summary>
    /// Implements rate limiting using a sliding window algorithm.
    /// Thread-safe for concurrent operations.
    /// </summary>
    public sealed class RateLimiter
    {
        private readonly int _maxRequestsPerWindow;
        private readonly TimeSpan _windowDuration;
        private readonly ConcurrentDictionary<string, Queue<DateTime>> _requestTimestamps = new();
        private readonly object _lock = new();

        public RateLimiter(int maxRequestsPerWindow, TimeSpan windowDuration)
        {
            if (maxRequestsPerWindow <= 0)
                throw new ArgumentException("Max requests must be positive", nameof(maxRequestsPerWindow));

            if (windowDuration <= TimeSpan.Zero)
                throw new ArgumentException("Window duration must be positive", nameof(windowDuration));

            _maxRequestsPerWindow = maxRequestsPerWindow;
            _windowDuration = windowDuration;
        }

        public void CheckRateLimit(string operationName)
        {
            var now = DateTime.UtcNow;

            lock (_lock)
            {
                var timestamps = _requestTimestamps.GetOrAdd(operationName, _ => new Queue<DateTime>());

                while (timestamps.Count > 0 && timestamps.Peek() <= now - _windowDuration)
                    timestamps.Dequeue();

                if (timestamps.Count >= _maxRequestsPerWindow)
                {
                    throw new InvalidOperationException(
                        $"Rate limit exceeded for '{operationName}'. " +
                        $"Max {_maxRequestsPerWindow} requests per {_windowDuration.TotalMinutes} minute(s).");
                }

                timestamps.Enqueue(now);
            }
        }

        public int GetCurrentRequestCount(string operationName)
        {
            var now = DateTime.UtcNow;
            lock (_lock)
            {
                if (!_requestTimestamps.TryGetValue(operationName, out var timestamps))
                    return 0;

                while (timestamps.Count > 0 && timestamps.Peek() <= now - _windowDuration)
                    timestamps.Dequeue();

                return timestamps.Count;
            }
        }

        public void Reset(string operationName)
        {
            lock (_lock)
            {
                _requestTimestamps.TryRemove(operationName, out _);
            }
        }

        public void ResetAll()
        {
            lock (_lock)
            {
                _requestTimestamps.Clear();
            }
        }
    }
}
```

### 3.3 ExecutionContext.cs

```csharp
namespace <ServerName>.Core
{
    /// <summary>
    /// Provides execution context with timeout support.
    /// </summary>
    public sealed class ExecutionContext
    {
        private readonly TimeSpan _timeout;

        public ExecutionContext(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
                throw new ArgumentException("Timeout must be positive", nameof(timeout));

            _timeout = timeout;
        }

        public T Execute<T>(Func<T> operation, string operationName)
        {
            using var cts = new CancellationTokenSource(_timeout);
            var task = Task.Run(operation, cts.Token);

            try
            {
                if (!task.Wait(_timeout))
                {
                    throw new TimeoutException(
                        $"Operation '{operationName}' exceeded timeout of {_timeout.TotalMilliseconds}ms.");
                }

                return task.Result;
            }
            catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
            {
                throw new TimeoutException(
                    $"Operation '{operationName}' was cancelled due to timeout.",
                    ex.InnerException);
            }
            catch (AggregateException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        public void Execute(Action operation, string operationName)
        {
            Execute(() => { operation(); return 0; }, operationName);
        }
    }
}
```

### 3.4 <ServerName>Tools.cs

```csharp
using ModelContextProtocol.Server;

namespace <ServerName>.Core
{
    /// <summary>
    /// Provides tool operations with resource limits and rate limiting.
    /// </summary>
    [McpServerToolType]
    public sealed partial class <ServerName>Tools
    {
        private static readonly RateLimiter _rateLimiter = new(
            maxRequestsPerWindow: 100,
            windowDuration: TimeSpan.FromMinutes(1)
        );

        /// <summary>
        /// Example operation. Replace with your actual tool operations.
        /// </summary>
        [McpServerTool]
        public double ExampleOperation(double input)
        {
            _rateLimiter.CheckRateLimit(nameof(ExampleOperation));
            ResourceLimits.ValidateValueRange(input, nameof(input));

            var result = input * 2; // Replace with actual logic

            ResourceLimits.ValidateResult(result, nameof(ExampleOperation));
            return result;
        }

        // Add more tool operations following the same pattern
    }
}
```

## Step 4: Create Console App (Stdio Transport)

### 4.1 Program.cs

```csharp
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using <ServerName>.Core;

[ExcludeFromCodeCoverage(Justification = "Composition root")]
internal partial class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // CRITICAL: Disable console logging - it corrupts JSON-RPC stdout
        builder.Logging.ClearProviders();

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<<ServerName>Tools>();

        await builder.Build().RunAsync();
    }
}
```

## Step 5: Create Web App (HTTP Transport)

### 5.1 Program.cs

```csharp
using System.Diagnostics.CodeAnalysis;
using <ServerName>.Core;
using <ServerName>.Web.Api;
using <ServerName>.Web.Authentication;

/// <summary>
/// Entry point for the Web application.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Composition root")]
public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddCalculatorAuthentication(builder.Configuration);

        builder.Services
            .AddMcpServer()
            .WithHttpTransport()
            .WithTools<<ServerName>Tools>();

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();

        var authType = builder.Configuration.GetValue<string>("Authentication:Type") ?? "ApiKey";

        var mcpEndpoint = app.MapMcp("/mcp");
        if (!string.Equals(authType, "None", StringComparison.OrdinalIgnoreCase))
            mcpEndpoint.RequireAuthorization();

        var apiGroup = app.Map<ServerName>Api();
        if (!string.Equals(authType, "None", StringComparison.OrdinalIgnoreCase))
            apiGroup.RequireAuthorization();

        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

        app.Run();
    }
}
```

### 5.2 appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Authentication": {
    "Type": "ApiKey",
    "ApiKey": {
      "HeaderName": "X-API-Key"
    },
    "Jwt": {
      "Issuer": "<ServerName>",
      "Audience": "<ServerName>Clients"
    }
  }
}
```

> **Security Note:** Never store secrets (API keys, JWT secret keys) in appsettings.json. Use User Secrets for development and environment variables for production:
> ```bash
> # Development
> dotnet user-secrets set "Authentication:ApiKey:ValidKeys:0" "your-api-key-here"
> dotnet user-secrets set "Authentication:Jwt:SecretKey" "your-32-character-minimum-secret-key"
> ```

### 5.3 appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "Authentication": {
    "Type": "None"
  }
}
```

## Step 6: Create Authentication Handlers

### 6.1 Configuration/ApiKeyAuthOptions.cs

```csharp
using Microsoft.AspNetCore.Authentication;

namespace <ServerName>.Web.Authentication.Configuration
{
    public class ApiKeyAuthOptions : AuthenticationSchemeOptions
    {
        public string HeaderName { get; set; } = "X-API-Key";
        public List<string> ValidKeys { get; set; } = new();
    }
}
```

### 6.2 ApiKeyAuthHandler.cs

```csharp
using System.Security.Claims;
using System.Text.Encodings.Web;
using <ServerName>.Web.Authentication.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace <ServerName>.Web.Authentication
{
    public class ApiKeyAuthHandler : AuthenticationHandler<ApiKeyAuthOptions>
    {
        public ApiKeyAuthHandler(
            IOptionsMonitor<ApiKeyAuthOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(Options.HeaderName, out var apiKeyValues))
            {
                return Task.FromResult(AuthenticateResult.Fail($"Missing {Options.HeaderName} header"));
            }

            var providedApiKey = apiKeyValues.FirstOrDefault()?.Trim();
            if (string.IsNullOrWhiteSpace(providedApiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("API key is empty"));
            }

            var isValid = Options.ValidKeys.Any(validKey =>
                string.Equals(validKey, providedApiKey, StringComparison.Ordinal));

            if (!isValid)
            {
                Logger.LogWarning("Invalid API key attempt: {KeyPrefix}****",
                    providedApiKey.Length >= 4 ? providedApiKey[..4] : "****");
                return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "ApiKeyClient"),
                new Claim(ClaimTypes.AuthenticationMethod, "ApiKey"),
                new Claim("ApiKeyPrefix", providedApiKey.Length >= 4 ? providedApiKey[..4] : "****")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.Headers["WWW-Authenticate"] = $"ApiKey header=\"{Options.HeaderName}\"";
            return Task.CompletedTask;
        }
    }
}
```

### 6.3 NoOpAuthHandler.cs

```csharp
using System.Security.Claims;
using System.Text.Encodings.Web;
using <ServerName>.Web.Authentication.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace <ServerName>.Web.Authentication
{
    /// <summary>
    /// No-op authentication handler that always succeeds.
    /// WARNING: Only use for development and testing.
    /// </summary>
    public class NoOpAuthHandler : ApiKeyAuthHandler
    {
        public NoOpAuthHandler(
            IOptionsMonitor<ApiKeyAuthOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

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
```

### 6.4 AuthenticationExtensions.cs

```csharp
using System.Text;
using <ServerName>.Web.Authentication.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace <ServerName>.Web.Authentication
{
    public static class AuthenticationExtensions
    {
        public static IServiceCollection AddCalculatorAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var authType = configuration.GetValue<string>("Authentication:Type") ?? "ApiKey";

            switch (authType.ToLowerInvariant())
            {
                case "none":
                    services.AddAuthentication("NoAuth")
                        .AddScheme<ApiKeyAuthOptions, NoOpAuthHandler>("NoAuth", options => { });
                    break;

                case "apikey":
                    var apiKeySection = configuration.GetSection("Authentication:ApiKey");
                    services.AddAuthentication("ApiKey")
                        .AddScheme<ApiKeyAuthOptions, ApiKeyAuthHandler>("ApiKey", options =>
                        {
                            options.HeaderName = apiKeySection.GetValue<string>("HeaderName") ?? "X-API-Key";
                            options.ValidKeys = apiKeySection.GetSection("ValidKeys").Get<List<string>>() ?? new();
                        });
                    break;

                case "jwt":
                    var jwtSection = configuration.GetSection("Authentication:Jwt");
                    var secretKey = jwtSection.GetValue<string>("SecretKey")
                        ?? throw new InvalidOperationException("JWT SecretKey is required");

                    if (secretKey.Length < 32)
                        throw new InvalidOperationException("JWT SecretKey must be at least 32 characters");

                    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                        .AddJwtBearer(options =>
                        {
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuer = true,
                                ValidateAudience = true,
                                ValidateLifetime = true,
                                ValidateIssuerSigningKey = true,
                                ValidIssuer = jwtSection.GetValue<string>("Issuer"),
                                ValidAudience = jwtSection.GetValue<string>("Audience"),
                                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                            };
                        });
                    break;

                case "windows":
                    services.AddAuthentication(Microsoft.AspNetCore.Authentication.Negotiate.NegotiateDefaults.AuthenticationScheme)
                        .AddNegotiate();
                    break;

                default:
                    throw new InvalidOperationException($"Unknown authentication type: {authType}");
            }

            services.AddAuthorization();
            return services;
        }
    }
}
```

## Step 7: Create REST API Endpoints

### 7.1 Api/Models/<ServerName>Request.cs

```csharp
using System.Diagnostics.CodeAnalysis;

namespace <ServerName>.Web.Api.Models
{
    [ExcludeFromCodeCoverage(Justification = "Model class")]
    public record <ServerName>Request(double Input);
}
```

### 7.2 Api/Models/<ServerName>Response.cs

```csharp
using System.Diagnostics.CodeAnalysis;

namespace <ServerName>.Web.Api.Models
{
    [ExcludeFromCodeCoverage(Justification = "Model class")]
    public record <ServerName>Response(double Result, string Operation);
}
```

### 7.3 Api/Models/<ServerName>ErrorResponse.cs

```csharp
using System.Diagnostics.CodeAnalysis;

namespace <ServerName>.Web.Api.Models
{
    [ExcludeFromCodeCoverage(Justification = "Model class")]
    public record <ServerName>ErrorResponse(string Error, string Message, string Operation);
}
```

### 7.4 Api/<ServerName>ApiEndpoints.cs

```csharp
using <ServerName>.Core;
using <ServerName>.Web.Api.Models;

namespace <ServerName>.Web.Api
{
    public static class <ServerName>ApiEndpoints
    {
        public static RouteGroupBuilder Map<ServerName>Api(this WebApplication app)
        {
            var tools = new <ServerName>Tools();
            var group = app.MapGroup("/api/<servername>").WithTags("<ServerName>");

            group.MapPost("/example", (<ServerName>Request request) =>
                ExecuteOperation(request, "Example", () => tools.ExampleOperation(request.Input)))
                .WithName("Example")
                .WithSummary("Example operation");

            return group;
        }

        private static IResult ExecuteOperation(
            <ServerName>Request request,
            string operationName,
            Func<double> operation)
        {
            try
            {
                var result = operation();
                return Results.Ok(new <ServerName>Response(result, operationName));
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new <ServerName>ErrorResponse(
                    "ValidationError", ex.Message, operationName));
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Rate limit"))
            {
                return Results.StatusCode(StatusCodes.Status429TooManyRequests);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new <ServerName>ErrorResponse(
                    "OverflowError", ex.Message, operationName));
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal Server Error");
            }
        }
    }
}
```

## Step 8: Create Test Files

### 8.1 coverlet.runsettings

```xml
<?xml version="1.0" encoding="utf-8" ?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage">
        <Configuration>
          <Exclude>
            [*]Program
          </Exclude>
          <ExcludeByFile>
            **/Program.cs
          </ExcludeByFile>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

### 8.2 Test Files

Create test files following these patterns (see McpCalculator reference for complete examples):
- `ResourceLimitsTests.cs`
- `RateLimiterTests.cs`
- `ExecutionContextTests.cs`
- `<ServerName>ToolsTests.cs`
- `ApiKeyAuthHandlerTests.cs`
- `AuthenticationExtensionsTests.cs`
- `NoOpAuthHandlerTests.cs`
- `<ServerName>ApiEndpointsTests.cs`

## Step 9: Build, Test, and Publish

### 9.1 Build

```bash
dotnet build
```

### 9.2 Run Tests with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage" --settings <ServerName>.Tests/coverlet.runsettings
```

### 9.3 Generate Coverage Report

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:Html
```

### 9.4 Publish

```bash
# Console app (stdio)
dotnet publish <ServerName>/<ServerName>.csproj -c Release -o publish/stdio

# Web app (HTTP)
dotnet publish <ServerName>.Web/<ServerName>.Web.csproj -c Release -o publish/web
```

## Step 10: Register and Deploy

### 10.1 Register Stdio Transport with Claude Code

```bash
claude mcp add --transport stdio --scope user <servername> -- "<path-to-publish>/stdio/<ServerName>.exe"
```

### 10.2 Deploy HTTP Transport

Options:
- **Kestrel**: Run `<ServerName>.Web.exe` directly
- **IIS**: Deploy to IIS with ASP.NET Core module
- **Docker**: Create Dockerfile and deploy to container service

## Checklist

Before considering complete, verify:

- [ ] All projects build without warnings
- [ ] All tests pass
- [ ] Code coverage is high (target 100% for Core, high for Web)
- [ ] XML documentation on all public methods
- [ ] Rate limiting configured appropriately
- [ ] Resource limits configured appropriately
- [ ] Authentication working for all configured types
- [ ] REST API endpoints documented
- [ ] Health endpoint responding
- [ ] Stdio transport registered and working with Claude Code
- [ ] HTTP transport deployed and accessible

## Common Pitfalls

1. **Console Logging**: Never use `Console.WriteLine()` in stdio transport - it corrupts MCP communication
2. **Program.cs Coverage**: Use `[ExcludeFromCodeCoverage]` - don't try to test composition roots
3. **Rate Limiting in Tests**: Use `--max-parallelization 1` for rate limit tests
4. **InternalsVisibleTo**: Remember to add for test project to access internal types
5. **JWT Secret Length**: Must be at least 32 characters
6. **JSON Serialization**: NaN/Infinity cannot be serialized to JSON

## Reference Implementation

This guide is based on the McpCalculator reference implementation:
- Location: `D:\projects\Claude\McpCalculator`
- 100 tests across 7 test classes
- 4 calculator operations
- 4 authentication types
- Complete REST API

---

**Generated:** 2026-01-29
**Version:** 2.0
**Based on:** McpCalculator reference implementation with multi-project architecture
