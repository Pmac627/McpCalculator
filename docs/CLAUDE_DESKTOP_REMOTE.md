# Remote MCP Server (HTTP/SSE)

This guide explains how to connect to the MCP BasicCalculator server over HTTP using Server-Sent Events (SSE) transport.

> **Important:** As of January 2025, Claude Desktop only supports **stdio (local) MCP servers**. The HTTP/SSE transport documented here is for use with other MCP clients (custom applications, web clients, or future Claude Desktop versions). For Claude Desktop, see [CLAUDE_DESKTOP_LOCAL.md](CLAUDE_DESKTOP_LOCAL.md).

## Overview

The **remote/HTTP** configuration runs the MCP server as a web service that clients connect to over HTTP/HTTPS. Communication uses JSON-RPC 2.0 with SSE for streaming.

```
┌─────────────────┐        HTTPS         ┌─────────────────┐
│   MCP Client    │ ◄───────────────────► │ McpCalculator   │
│  (HTTP-capable) │    (HTTP/SSE)        │ .Web Server     │
└─────────────────┘                       └─────────────────┘
```

**Advantages:**
- Server can run on a different machine
- Multiple clients can share one server
- Full authentication support (API Key, JWT, Windows)
- Server can be scaled and load-balanced

**Disadvantages:**
- Requires network access to the server
- Authentication configuration required
- HTTPS recommended for production

## Prerequisites

1. **.NET 10 SDK** installed on the server
2. **McpCalculator.Web** project deployed and running
3. Network access from client to the server

### Start the Server

#### Development Mode (Local)

```bash
cd McpCalculator.Web
dotnet run
```

Default URL: `https://localhost:5001`

#### Production Deployment

See [IIS Deployment Guide](IIS_DEPLOYMENT.md) for deploying to IIS or other hosting options.

## Server Configuration

### Authentication Options

#### No Authentication (Development Only)

Configure the server for no authentication in `appsettings.json`:

```json
{
  "Authentication": {
    "Type": "None"
  }
}
```

#### API Key Authentication (Recommended)

**Server Configuration** (`appsettings.json`):

```json
{
  "Authentication": {
    "Type": "ApiKey",
    "ApiKey": {
      "HeaderName": "X-API-Key"
    }
  }
}
```

> **Security Note:** Store API keys in User Secrets (development) or environment variables (production), not in appsettings.json. See [AUTHENTICATION.md](AUTHENTICATION.md) for details.
>
> ```bash
> # Development: Use User Secrets
> dotnet user-secrets set "Authentication:ApiKey:ValidKeys:0" "my-secure-api-key-12345"
> ```

**Test with curl:**

```bash
curl -X POST https://localhost:5001/mcp \
  -H "Content-Type: application/json" \
  -H "X-API-Key: my-secure-api-key-12345" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

#### JWT Authentication

**Server Configuration** (`appsettings.json`):

```json
{
  "Authentication": {
    "Type": "Jwt",
    "Jwt": {
      "Issuer": "McpCalculator",
      "Audience": "McpCalculatorClients"
    }
  }
}
```

> **Security Note:** Store the JWT secret key in User Secrets (development) or environment variables (production):
>
> ```bash
> dotnet user-secrets set "Authentication:Jwt:SecretKey" "your-secret-key-must-be-at-least-32-characters-long"
> ```

**Test with curl:**

```bash
curl -X POST https://localhost:5001/mcp \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <your-jwt-token>" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

> **Note:** You'll need to generate a valid JWT token using the same secret key, issuer, and audience configured on the server.

## MCP Client Configuration (Future)

> **Note:** The following configuration format is for MCP clients that support HTTP transport. As of January 2025, Claude Desktop only supports stdio. This section documents the expected format for future compatibility.

### Configuration Schema

```json
{
  "mcpServers": {
    "BasicCalculator": {
      "url": "https://your-server.com/mcp",
      "headers": {
        "X-API-Key": "your-api-key"
      }
    }
  }
}
```

| Property | Description | Required |
|----------|-------------|----------|
| `url` | Full URL to the MCP endpoint | Yes |
| `headers` | HTTP headers to include with requests | No |

## Available Tools

Once configured, the following BasicCalculator tools become available:

| Tool | Description | Parameters |
|------|-------------|------------|
| `Add` | Adds two numbers | `a` (number), `b` (number) |
| `Subtract` | Subtracts b from a | `a` (number), `b` (number) |
| `Multiply` | Multiplies two numbers | `a` (number), `b` (number) |
| `Divide` | Divides a by b | `a` (number), `b` (number) |

## Verify Server is Running

Verify the server is accessible before connecting clients:

### Health Check

```bash
curl https://localhost:5001/health
```

Expected response:
```json
{"status":"healthy","timestamp":"2025-01-31T12:00:00Z"}
```

### Server Info

```bash
curl https://localhost:5001/info
```

Expected response:
```json
{
  "name": "McpCalculator",
  "version": "1.0.0",
  "transports": {
    "mcp": "HTTP/SSE (Model Context Protocol)",
    "rest": "REST API (JSON)"
  },
  "authentication": "ApiKey",
  "endpoints": {
    "mcp": "/mcp",
    "health": "/health",
    "info": "/info"
  }
}
```

### Test MCP Endpoint

```bash
# With API Key authentication
curl -X POST https://localhost:5001/mcp \
  -H "Content-Type: application/json" \
  -H "X-API-Key: my-secure-api-key-12345" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

## Troubleshooting

### Server Not Connecting

1. **Verify server is running** - Check health endpoint
2. **Check URL** - Ensure `/mcp` path is included
3. **Check authentication** - Verify headers match server config
4. **Check network** - Ensure no firewall blocking

### Authentication Errors (401)

| Issue | Solution |
|-------|----------|
| Missing API key | Add `X-API-Key` header |
| Wrong API key | Check key matches server config |
| Wrong header name | Check `HeaderName` in server config |
| Expired JWT | Generate new token |

### Connection Errors

| Error | Cause | Solution |
|-------|-------|----------|
| "Connection refused" | Server not running | Start the server |
| "Connection timeout" | Network issue | Check firewall/network |
| "SSL certificate error" | Invalid certificate | Use valid cert or HTTP for dev |
| "404 Not Found" | Wrong URL | Add `/mcp` to the URL |

### Check Server Logs

The web server logs requests and errors:

```bash
# Development mode logs to console
dotnet run

# Check for authentication failures
# Check for MCP protocol errors
```

## Security Best Practices

### Production Checklist

- [ ] Use HTTPS with valid SSL certificate
- [ ] Use strong API keys (32+ characters, random)
- [ ] Rotate API keys periodically
- [ ] Use JWT with short expiration for sensitive operations
- [ ] Deploy behind a reverse proxy (nginx, IIS ARR)
- [ ] Enable rate limiting
- [ ] Monitor and log access

### API Key Generation

Generate secure API keys:

```bash
# PowerShell
[System.Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))

# Bash
openssl rand -base64 32
```

### Firewall Configuration

Only expose the server to trusted networks:

```bash
# Example: Only allow localhost
netsh advfirewall firewall add rule name="MCP Server" dir=in action=allow protocol=TCP localport=5001 remoteip=127.0.0.1
```

## Using HTTP Transport with Custom Clients

The HTTP/SSE transport is useful for:
- Custom MCP client applications
- Web-based integrations
- Server-to-server communication
- Load-balanced deployments
- Scenarios requiring centralized authentication

For Claude Desktop, use the [stdio transport](CLAUDE_DESKTOP_LOCAL.md) instead.

## Related Documentation

- [Local Configuration Guide](CLAUDE_DESKTOP_LOCAL.md) - For stdio transport
- [HTTP Transport Technical Details](HTTP_TRANSPORT.md) - Protocol details
- [Authentication Guide](AUTHENTICATION.md) - Auth configuration options
- [IIS Deployment Guide](IIS_DEPLOYMENT.md) - Production deployment
- [REST API Documentation](REST_API.md) - Traditional REST endpoints
