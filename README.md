# MCP Calculator

A comprehensive Model Context Protocol (MCP) server implementation demonstrating best practices for building secure, testable, and production-ready MCP servers in .NET.

## Purpose

This project serves as both a **learning resource** and a **bootstrap template** for developers looking to:

- Understand the Model Context Protocol specification
- Build MCP servers with dual transport support (stdio and HTTP/SSE)
- Implement enterprise-grade authentication and security
- Structure .NET projects with proper separation of concerns
- Achieve high test coverage with unit and integration tests

> [!WARNING]
> This code is provided as a basic example and is not guaranteed to be up to date or hardened against a constantly evolving threat landscape.

## Features

### Calculator Operations
Four basic arithmetic operations exposed as MCP tools:
- **Add** - Add two numbers
- **Subtract** - Subtract second number from first
- **Multiply** - Multiply two numbers
- **Divide** - Divide first number by second

### Dual Transport Support

| Transport | Project | Use Case |
|-----------|---------|----------|
| **stdio** | McpCalculator | Local integration with Claude Desktop |
| **HTTP/SSE** | McpCalculator.Web | Network deployment, web clients, custom MCP clients |

### Security Features
- **Input Validation** - NaN/Infinity detection, range validation (±1e15)
- **Rate Limiting** - 100 requests/minute per operation with sliding window
- **Resource Limits** - Overflow protection, minimum denominator enforcement
- **Authentication** - Four authentication modes (see below)

### Authentication Options (HTTP/SSE)
| Type | Description |
|------|-------------|
| **None** | Development only, no authentication |
| **ApiKey** | Custom header validation with timing-safe comparison |
| **JWT** | Bearer token with configurable issuer/audience |
| **Windows** | NTLM/Kerberos via IIS Negotiate |

## Project Structure

```
McpCalculator/
├── McpCalculator.Core/        # Shared calculator logic library
│   ├── CalculatorTools.cs     # MCP tool definitions
│   ├── ResourceLimits.cs      # Value validation
│   ├── RateLimiter.cs         # Rate limiting implementation
│   └── ExecutionContext.cs    # Timeout enforcement
│
├── McpCalculator/             # Stdio MCP Server (Console App)
│   └── Program.cs             # Entry point for Claude Desktop
│
├── McpCalculator.Web/         # HTTP/SSE MCP Server (ASP.NET Core)
│   ├── Program.cs             # Web application entry point
│   ├── Api/                   # REST API endpoints
│   └── Authentication/        # Auth handlers and configuration
│
├── McpCalculator.Tests/       # Unit and Integration Tests
│   └── 100+ tests with xUnit
│
└── docs/                      # Documentation
    ├── AUTHENTICATION.md      # Authentication configuration guide
    ├── REST_API.md            # REST endpoint documentation
    ├── HTTP_TRANSPORT.md      # MCP over HTTP/SSE guide
    ├── CLAUDE_DESKTOP_LOCAL.md    # Claude Desktop stdio setup
    ├── CLAUDE_DESKTOP_REMOTE.md   # Custom MCP client setup
    ├── IIS_DEPLOYMENT.md      # Production deployment guide
    └── MCP_SERVER_SCAFFOLD_GUIDE.md  # Create your own MCP server
```

## Quick Start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Claude Desktop](https://claude.ai/download) (for local MCP integration)

### Build the Solution
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Option 1: Local MCP Server (Claude Desktop)

1. Publish the stdio server:
   ```bash
   dotnet publish McpCalculator -c Release -r win-x64 --self-contained true
   ```

2. Configure Claude Desktop (`%APPDATA%\Claude\claude_desktop_config.json`):
   ```json
   {
     "mcpServers": {
       "BasicCalculator": {
         "command": "C:\\path\\to\\McpCalculator\\bin\\Release\\net10.0\\win-x64\\publish\\McpCalculator.exe",
         "args": []
       }
     }
   }
   ```

3. Restart Claude Desktop

### Option 2: HTTP/SSE MCP Server

1. Configure authentication (using User Secrets for development):
   ```bash
   cd McpCalculator.Web
   dotnet user-secrets set "Authentication:ApiKey:ValidKeys:0" "your-secret-key"
   ```

2. Run the web server:
   ```bash
   dotnet run --project McpCalculator.Web
   ```

3. Test with curl:
   ```bash
   curl -X POST https://localhost:5001/api/calculator/add \
     -H "Content-Type: application/json" \
     -H "X-API-Key: your-secret-key" \
     -d '{"a": 5, "b": 3}'
   ```

## Configuration

### Configuration Sources (in order of precedence)
1. `appsettings.json` / `appsettings.{Environment}.json`
2. User Secrets (Development only)
3. Environment Variables
4. Azure Key Vault (optional, requires additional package)

### Environment Variables Example
```bash
# Authentication type
Authentication__Type=ApiKey

# API Keys
Authentication__ApiKey__ValidKeys__0=key1
Authentication__ApiKey__ValidKeys__1=key2

# JWT Settings
Authentication__Jwt__SecretKey=your-256-bit-secret
```

## API Endpoints (HTTP/SSE Server)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/calculator/add` | POST | Add two numbers |
| `/api/calculator/subtract` | POST | Subtract two numbers |
| `/api/calculator/multiply` | POST | Multiply two numbers |
| `/api/calculator/divide` | POST | Divide two numbers |
| `/health` | GET | Health check |
| `/info` | GET | Server information |
| `/mcp` | - | MCP SSE endpoint |

### Request/Response Format
```json
// Request
{ "a": 10, "b": 5 }

// Success Response
{ "result": 15, "operation": "Add" }

// Error Response
{ "error": "ValidationError", "message": "...", "operation": "Add" }
```

## Testing

The project includes 100+ tests covering:
- Resource validation and limits
- Rate limiting behavior
- Authentication handlers
- REST API endpoints
- Integration tests with WebApplicationFactory

### Run Tests with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Generate Coverage Report
```bash
# Install ReportGenerator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

## Security Considerations

### Numerical Constraints
| Constraint | Value | Purpose |
|-----------|-------|---------|
| Max Input Value | ±1e15 | Prevent multiplication overflow |
| Min Denominator | 1e-10 | Prevent division overflow |
| Precision | 15 digits | IEEE 754 double precision |

### Rate Limiting
- 100 requests per minute per operation
- Sliding window algorithm
- Thread-safe implementation
- Independent counters per operation

### API Key Security
- Constant-time comparison (timing attack prevention)
- Masked logging for audit trails
- Multiple valid keys supported

## Technologies

- **.NET 10** - Latest LTS framework
- **Model Context Protocol** - MCP SDK for .NET
- **ASP.NET Core** - Web framework for HTTP/SSE transport
- **xUnit** - Testing framework
- **Moq** - Mocking library
- **Coverlet** - Code coverage

## Documentation

| Document | Description |
|----------|-------------|
| [Authentication Guide](docs/AUTHENTICATION.md) | Configure authentication options |
| [REST API Reference](docs/REST_API.md) | API endpoints and examples |
| [HTTP Transport](docs/HTTP_TRANSPORT.md) | MCP over HTTP/SSE explained |
| [Claude Desktop (Local)](docs/CLAUDE_DESKTOP_LOCAL.md) | Stdio setup for Claude Desktop |
| [Custom MCP Clients](docs/CLAUDE_DESKTOP_REMOTE.md) | HTTP/SSE for custom clients |
| [IIS Deployment](docs/IIS_DEPLOYMENT.md) | Production deployment guide |
| [Scaffold Guide](docs/MCP_SERVER_SCAFFOLD_GUIDE.md) | Build your own MCP server |

## License

This project is licensed under the [MIT License](LICENSE).

## Resources

- [Model Context Protocol Specification](https://spec.modelcontextprotocol.io/)
- [MCP .NET SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [Claude Desktop](https://claude.ai/download)
