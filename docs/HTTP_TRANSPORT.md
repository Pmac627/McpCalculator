# MCP HTTP Transport Guide

This document explains how the MCP Calculator server uses HTTP/SSE transport for web-based deployments.

## Overview

The Model Context Protocol (MCP) supports multiple transport mechanisms:

| Transport | Use Case | Protocol |
|-----------|----------|----------|
| **stdio** | Local process communication | JSON-RPC 2.0 over stdin/stdout |
| **HTTP/SSE** | Network/web deployment | JSON-RPC 2.0 over HTTP with Server-Sent Events |

This project supports both transports:
- `McpCalculator` - Uses stdio transport (for Claude Desktop)
- `McpCalculator.Web` - Uses HTTP/SSE transport (for web deployment)

## How HTTP/SSE Transport Works

### Request Flow

```
┌─────────────┐                    ┌─────────────────┐
│  MCP Client │                    │ McpCalculator   │
│             │                    │ .Web Server     │
└──────┬──────┘                    └────────┬────────┘
       │                                    │
       │  1. POST /mcp (initialize)         │
       │ ──────────────────────────────────>│
       │                                    │
       │  2. Response (session info)        │
       │ <──────────────────────────────────│
       │                                    │
       │  3. POST /mcp (tool call)          │
       │ ──────────────────────────────────>│
       │                                    │
       │  4. Response (tool result)         │
       │ <──────────────────────────────────│
       │                                    │
```

### Endpoints

The `app.MapMcp("/mcp")` call creates the following endpoints:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/mcp` | POST | Main MCP endpoint for JSON-RPC requests |
| `/mcp/sse` | GET | Server-Sent Events stream (legacy clients) |

### JSON-RPC Protocol

All MCP communication uses JSON-RPC 2.0:

**Request Format:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "Add",
    "arguments": {
      "a": 5,
      "b": 3
    }
  }
}
```

**Response Format:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "8"
      }
    ]
  }
}
```

## Configuration

### Program.cs Setup

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add MCP server with HTTP transport
builder.Services
    .AddMcpServer()
    .WithHttpTransport()  // Enable HTTP/SSE transport
    .WithTools<CalculatorTools>();

var app = builder.Build();

// Map MCP endpoints
app.MapMcp("/mcp");

app.Run();
```

### WithHttpTransport Options

The `WithHttpTransport()` method accepts optional configuration:

```csharp
builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        // Session timeout (default: 2 hours)
        options.IdleTimeout = TimeSpan.FromMinutes(30);

        // For load balancing without sticky sessions
        options.Stateless = true;

        // Maximum idle sessions in memory (default: 10,000)
        options.MaxIdleSessionCount = 5000;
    })
    .WithTools<CalculatorTools>();
```

| Option | Default | Description |
|--------|---------|-------------|
| `IdleTimeout` | 2 hours | Time before idle sessions are cleaned up |
| `Stateless` | false | Enable stateless mode for load balancing |
| `MaxIdleSessionCount` | 10,000 | Maximum tracked idle sessions |

## Server-Sent Events (SSE)

SSE enables the server to push messages to the client over a persistent HTTP connection.

### How SSE Works

1. Client opens a connection to `/mcp/sse`
2. Server keeps connection open
3. Server sends events as they occur
4. Client receives events in real-time

### SSE Message Format

```
event: message
data: {"jsonrpc":"2.0","id":1,"result":{"content":[{"type":"text","text":"8"}]}}

```

### Browser Compatibility

SSE is supported in all modern browsers:
- Chrome, Edge, Firefox, Safari
- Automatic reconnection on connection loss
- No special client library required

## Testing the HTTP Endpoint

### Using curl

```bash
# Health check
curl https://localhost:5001/health

# Server info
curl https://localhost:5001/info

# List available tools
curl -X POST https://localhost:5001/mcp \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key-here" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'

# Call the Add tool
curl -X POST https://localhost:5001/mcp \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key-here" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"Add","arguments":{"a":5,"b":3}}}'
```

### Using PowerShell

```powershell
# Health check
Invoke-RestMethod -Uri "https://localhost:5001/health"

# Call the Add tool
$body = @{
    jsonrpc = "2.0"
    id = 1
    method = "tools/call"
    params = @{
        name = "Add"
        arguments = @{ a = 5; b = 3 }
    }
} | ConvertTo-Json -Depth 5

Invoke-RestMethod -Uri "https://localhost:5001/mcp" `
    -Method POST `
    -ContentType "application/json" `
    -Headers @{ "X-API-Key" = "your-api-key-here" } `
    -Body $body
```

## Differences from Stdio Transport

| Aspect | Stdio | HTTP/SSE |
|--------|-------|----------|
| **Process Model** | Server runs as child process | Server runs as web service |
| **Connection** | Process pipes (stdin/stdout) | HTTP connections |
| **Logging** | Must be disabled (corrupts protocol) | Can be enabled |
| **Network** | Local only | Remote clients supported |
| **Authentication** | Process-level security | HTTP auth (API Key, JWT, etc.) |
| **Scaling** | One process per client | Multiple clients per server |

## Troubleshooting

### Connection Issues

1. **SSE connection drops**: Check IIS WebSocket support
2. **CORS errors**: Configure CORS in Program.cs
3. **Timeout issues**: Adjust `IdleTimeout` setting

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| 401 Unauthorized | Missing/invalid auth | Check API key or JWT |
| 404 Not Found | Wrong endpoint | Use `/mcp` not `/api` |
| 500 Server Error | Tool exception | Check server logs |

## NuGet Packages

The HTTP transport requires the ASP.NET Core extension package:

```xml
<PackageReference Include="ModelContextProtocol.AspNetCore" Version="0.3.0-preview.3" />
```

This package provides:
- `WithHttpTransport()` extension method
- `MapMcp()` endpoint mapping
- SSE infrastructure

## Further Reading

- [MCP Specification](https://spec.modelcontextprotocol.io/)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [Server-Sent Events MDN](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events)
