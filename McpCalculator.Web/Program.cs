using System.Diagnostics.CodeAnalysis;
using McpCalculator.Core;
using McpCalculator.Web.Api;
using McpCalculator.Web.Authentication.Configuration;
using Microsoft.AspNetCore.HttpOverrides;

/// <summary>
/// Entry point for the MCP Calculator Web application.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Composition root")]
public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configuration sources (in order of precedence):
        // 1. appsettings.json / appsettings.{Environment}.json
        // 2. User Secrets (Development only): dotnet user-secrets set "Authentication:ApiKey:ValidKeys:0" "key"
        // 3. Environment variables: Authentication__ApiKey__ValidKeys__0=key
        // 4. Azure Key Vault (add Azure.Extensions.AspNetCore.Configuration.Secrets package):
        //    builder.Configuration.AddAzureKeyVault(new Uri("https://your-vault.vault.azure.net/"), new DefaultAzureCredential());

        // Configure authentication based on configuration
        // Options: "None", "ApiKey", "Jwt", "Windows"
        builder.Services.AddCalculatorAuthentication(builder.Configuration);

        // Add MCP server with HTTP transport (SSE-based)
        // This enables remote MCP clients to connect over HTTP/HTTPS
        builder.Services
            .AddMcpServer()
            .WithHttpTransport()
            .WithTools<CalculatorTools>();

        var app = builder.Build();

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        // Security middleware pipeline
        app.UseAuthentication();
        app.UseAuthorization();

        // Map MCP endpoints at /mcp
        // - POST /mcp - Initialize MCP session and handle requests
        // - GET /mcp/sse - Server-Sent Events stream for responses (legacy)
        // Authentication is required unless configured as "None"
        var mcpEndpoint = app.MapMcp("/mcp");

        // Only require authorization if authentication is not "None"
        var authType = builder.Configuration.GetValue<string>("Authentication:Type") ?? "ApiKey";
        if (!string.Equals(authType, "None", StringComparison.OrdinalIgnoreCase))
        {
            mcpEndpoint.RequireAuthorization();
        }

        // Map REST API endpoints under /api/calculator
        // These provide a traditional REST interface alongside MCP
        var apiGroup = app.MapCalculatorApi();

        // Apply same authorization to REST API as MCP (unless auth is "None")
        if (!string.Equals(authType, "None", StringComparison.OrdinalIgnoreCase))
        {
            apiGroup.RequireAuthorization();
        }

        // Health check endpoint (no authentication required)
        // Use this to verify the server is running
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

        // Info endpoint showing server configuration (no auth)
        app.MapGet("/info", () => Results.Ok(new
        {
            name = "McpCalculator",
            version = "1.0.0",
            transports = new
            {
                mcp = "HTTP/SSE (Model Context Protocol)",
                rest = "REST API (JSON)"
            },
            authentication = authType,
            endpoints = new
            {
                mcp = "/mcp",
                api = new
                {
                    add = "POST /api/calculator/add",
                    subtract = "POST /api/calculator/subtract",
                    multiply = "POST /api/calculator/multiply",
                    divide = "POST /api/calculator/divide"
                },
                health = "/health",
                info = "/info"
            }
        }));

        app.Run();
    }
}
