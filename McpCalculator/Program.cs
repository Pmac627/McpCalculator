using System.Diagnostics.CodeAnalysis;
using McpCalculator.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

[ExcludeFromCodeCoverage(Justification = "Composition root")]
internal partial class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Disable console logging - it corrupts the JSON-RPC stdout communication.
        builder.Logging.ClearProviders();

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<CalculatorTools>();

        await builder.Build().RunAsync();
    }
}