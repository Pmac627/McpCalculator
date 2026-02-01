# Authentication Guide

This document explains the authentication options available in the MCP Calculator Web server.

## Overview

The server supports four authentication types, configurable via `appsettings.json`:

| Type | Header | Use Case | Security Level |
|------|--------|----------|----------------|
| **None** | - | Development only | None |
| **ApiKey** | `X-API-Key` | Internal services | Basic |
| **Jwt** | `Authorization: Bearer` | Web/API clients | Standard |
| **Windows** | (Automatic) | IIS intranet | Enterprise |

## Configuration

Set the authentication type in `appsettings.json`:

```json
{
  "Authentication": {
    "Type": "ApiKey"
  }
}
```

---

## Configuration Sources

ASP.NET Core supports multiple configuration sources, loaded in order of precedence (later sources override earlier ones):

| Source | Precedence | Use Case |
|--------|------------|----------|
| `appsettings.json` | 1 (lowest) | Default/template values |
| `appsettings.{Environment}.json` | 2 | Environment-specific overrides |
| User Secrets | 3 | Development secrets (local only) |
| Environment Variables | 4 | Production secrets, containers |
| Azure Key Vault | 5 (highest) | Enterprise secret management |

### User Secrets (Recommended for Development)

User Secrets store sensitive configuration outside your project directory, preventing accidental commits.

**Initialize user secrets:**
```bash
cd McpCalculator.Web
dotnet user-secrets init
```

**Set API keys:**
```bash
dotnet user-secrets set "Authentication:ApiKey:ValidKeys:0" "dev-api-key-12345"
dotnet user-secrets set "Authentication:ApiKey:ValidKeys:1" "dev-api-key-67890"
```

**Set JWT secret:**
```bash
dotnet user-secrets set "Authentication:Jwt:SecretKey" "your-dev-jwt-secret-key-at-least-32-chars"
```

**View all secrets:**
```bash
dotnet user-secrets list
```

**Remove a secret:**
```bash
dotnet user-secrets remove "Authentication:ApiKey:ValidKeys:0"
```

> **Note:** User secrets are only loaded when `ASPNETCORE_ENVIRONMENT=Development`. They are stored in:
> - Windows: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
> - macOS/Linux: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

### Environment Variables (Recommended for Production)

Environment variables are ideal for production deployments, containers, and CI/CD pipelines.

Use `__` (double underscore) as the hierarchy separator:

**Windows (PowerShell):**
```powershell
$env:Authentication__ApiKey__ValidKeys__0 = "prod-api-key-12345"
$env:Authentication__ApiKey__ValidKeys__1 = "prod-api-key-67890"
$env:Authentication__Jwt__SecretKey = "your-prod-jwt-secret-key-at-least-32-chars"
```

**Windows (Command Prompt):**
```cmd
set Authentication__ApiKey__ValidKeys__0=prod-api-key-12345
set Authentication__ApiKey__ValidKeys__1=prod-api-key-67890
```

**Linux/macOS:**
```bash
export Authentication__ApiKey__ValidKeys__0="prod-api-key-12345"
export Authentication__ApiKey__ValidKeys__1="prod-api-key-67890"
export Authentication__Jwt__SecretKey="your-prod-jwt-secret-key-at-least-32-chars"
```

**Docker:**
```dockerfile
ENV Authentication__ApiKey__ValidKeys__0=prod-api-key-12345
```

**Docker Compose:**
```yaml
services:
  mcp-calculator:
    environment:
      - Authentication__ApiKey__ValidKeys__0=prod-api-key-12345
      - Authentication__Jwt__SecretKey=your-prod-jwt-secret
```

### Azure Key Vault (Enterprise)

For enterprise deployments, Azure Key Vault provides centralized secret management with auditing, rotation, and access policies.

**1. Add the NuGet package:**
```bash
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
```

**2. Configure in Program.cs:**
```csharp
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add Azure Key Vault as a configuration source
var keyVaultUrl = builder.Configuration["KeyVault:Url"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential());
}
```

**3. Store secrets in Key Vault:**
Key Vault secret names use `--` instead of `:` for hierarchy:
- `Authentication--ApiKey--ValidKeys--0`
- `Authentication--Jwt--SecretKey`

---

## Authentication Type: None

**Use Case:** Local development and testing only.

```json
{
  "Authentication": {
    "Type": "None"
  }
}
```

**Behavior:**
- All requests are allowed without authentication
- A placeholder identity is created for authorization middleware compatibility

**Example Request:**
```bash
curl -X POST https://localhost:5001/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

> **WARNING:** Never use `Type: None` in production. It allows anyone to access your server.

---

## Authentication Type: ApiKey

**Use Case:** Simple internal services, server-to-server communication.

### Configuration

**appsettings.json** (template only, no secrets):
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

**Add keys via User Secrets (development):**
```bash
dotnet user-secrets set "Authentication:ApiKey:ValidKeys:0" "your-api-key-here"
dotnet user-secrets set "Authentication:ApiKey:ValidKeys:1" "another-valid-key"
```

**Or via Environment Variables (production):**
```bash
export Authentication__ApiKey__ValidKeys__0="your-api-key-here"
export Authentication__ApiKey__ValidKeys__1="another-valid-key"
```

| Setting | Description | Default |
|---------|-------------|---------|
| `HeaderName` | HTTP header name for the API key | `X-API-Key` |
| `ValidKeys` | Array of valid API keys (use secrets, not appsettings!) | Empty array |

### How It Works

1. Client includes API key in request header
2. Server validates key against `ValidKeys` array
3. Uses constant-time comparison to prevent timing attacks
4. Returns 401 if key is missing or invalid

### Example Request

```bash
curl -X POST https://localhost:5001/mcp \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key-here" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

### Security Considerations

1. **Use HTTPS:** API keys sent over HTTP can be intercepted
2. **Rotate keys periodically:** Change keys every 30-90 days
3. **Use unique keys per client:** Easier to revoke compromised keys
4. **Store keys securely:** Use environment variables or secret managers in production

### Generating API Keys

Use a cryptographically secure random generator:

```powershell
# PowerShell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

```bash
# Bash
openssl rand -base64 32
```

---

## Authentication Type: Jwt

**Use Case:** Standard web API authentication, external clients, OAuth integration.

### Configuration

**appsettings.json** (template only, no secrets):
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

**Add secret key via User Secrets (development):**
```bash
dotnet user-secrets set "Authentication:Jwt:SecretKey" "your-secret-key-must-be-at-least-32-characters-long"
```

**Or via Environment Variables (production):**
```bash
export Authentication__Jwt__SecretKey="your-secret-key-must-be-at-least-32-characters-long"
```

| Setting | Description | Requirement |
|---------|-------------|-------------|
| `Issuer` | Token issuer identifier | Required |
| `Audience` | Intended audience | Required |
| `SecretKey` | HMAC signing key (use secrets, not appsettings!) | Min 32 characters |

### How It Works

1. Client obtains JWT from identity provider (or generates it)
2. Client includes JWT in `Authorization` header
3. Server validates signature, issuer, audience, and expiration
4. Returns 401 if token is invalid or expired

### Example Request

```bash
curl -X POST https://localhost:5001/mcp \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

### Generating a Test JWT

For testing, you can generate a JWT using various tools:

**Using jwt.io:**
1. Go to https://jwt.io/
2. Select HS256 algorithm
3. Set payload with `iss`, `aud`, and `exp` claims
4. Enter your secret key
5. Copy the generated token

**JWT Payload Example:**
```json
{
  "sub": "test-client",
  "iss": "McpCalculator",
  "aud": "McpCalculatorClients",
  "exp": 1735689600,
  "iat": 1704067200
}
```

**Using C#:**
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

var key = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes("your-secret-key-must-be-at-least-32-characters-long"));

var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

var token = new JwtSecurityToken(
    issuer: "McpCalculator",
    audience: "McpCalculatorClients",
    claims: new[] { new Claim("sub", "client-id") },
    expires: DateTime.UtcNow.AddHours(1),
    signingCredentials: credentials);

var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
```

### Security Considerations

1. **Use strong secret keys:** At least 32 bytes (256 bits) of entropy
2. **Set appropriate expiration:** Balance security vs. user experience
3. **Use HTTPS:** Tokens can be stolen if intercepted
4. **Consider RS256:** For production, asymmetric keys (RS256) are more secure than HMAC (HS256)

---

## Authentication Type: Windows

**Use Case:** IIS-hosted intranet applications with Active Directory.

### Configuration

```json
{
  "Authentication": {
    "Type": "Windows"
  }
}
```

No additional configuration is needed - IIS handles authentication automatically.

### IIS Setup Required

1. **Enable Windows Authentication in IIS:**
   - Open IIS Manager
   - Select your site
   - Open Authentication
   - Enable "Windows Authentication"
   - Disable "Anonymous Authentication"

2. **Install the Negotiate module (if not present):**
   - Windows Features → Internet Information Services
   - → Web Management Tools → IIS 6 Management Compatibility
   - → Windows Authentication

### How It Works

1. Client (browser/app) sends request
2. IIS challenges client for Windows credentials
3. Client responds with Kerberos ticket or NTLM hash
4. IIS validates credentials against Active Directory
5. User identity is passed to ASP.NET Core

### Example Request (Browser)

Browsers automatically handle Windows authentication when:
- User is on the same domain
- Site is in the Intranet zone (IE/Edge)
- Credentials are available

### Example Request (PowerShell)

```powershell
Invoke-RestMethod -Uri "http://intranet-server/mcp" `
  -Method POST `
  -UseDefaultCredentials `
  -ContentType "application/json" `
  -Body '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

### Accessing User Identity

In the server code, access the authenticated user:

```csharp
var username = context.User.Identity?.Name;  // DOMAIN\username
```

---

## Switching Between Environments

Use different configurations for different environments:

**appsettings.json (Base configuration):**
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

**appsettings.Development.json (Development - no auth for convenience):**
```json
{
  "Authentication": {
    "Type": "None"
  }
}
```

**Production secrets via environment variables:**
```bash
export Authentication__ApiKey__ValidKeys__0="production-key-1"
export Authentication__ApiKey__ValidKeys__1="production-key-2"
```

The configuration is loaded based on the `ASPNETCORE_ENVIRONMENT` variable, with secrets from environment variables or Key Vault.

---

## Error Responses

### 401 Unauthorized

Returned when authentication fails:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
  "title": "Unauthorized",
  "status": 401
}
```

### Response Headers

| Auth Type | WWW-Authenticate Header |
|-----------|------------------------|
| ApiKey | `ApiKey realm="McpCalculator", header="X-API-Key"` |
| Jwt | `Bearer error="invalid_token"` |
| Windows | `Negotiate` |

---

## Best Practices

1. **Always use HTTPS in production** - All auth methods transmit credentials
2. **Use environment-specific config** - Different auth for dev/staging/prod
3. **Log authentication failures** - Monitor for brute force attempts
4. **Implement rate limiting** - Prevent credential stuffing attacks
5. **Rotate secrets regularly** - API keys and JWT secrets should be rotated
6. **Use secret management** - Azure Key Vault, AWS Secrets Manager, etc.

---

## Comparison Matrix

| Feature | None | ApiKey | JWT | Windows |
|---------|------|--------|-----|---------|
| Setup Complexity | None | Low | Medium | Medium |
| External Clients | Yes | Yes | Yes | No |
| Token Expiration | N/A | Manual | Built-in | N/A |
| Identity Claims | No | Limited | Full | Full |
| Suitable for Production | No | Yes | Yes | Yes |
| Requires IIS | No | No | No | Yes |
