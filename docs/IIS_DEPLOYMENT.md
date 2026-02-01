# IIS Deployment Guide

This document provides step-by-step instructions for deploying the MCP Calculator Web server to IIS.

## Prerequisites

Before deploying, ensure the following are installed on your IIS server:

### 1. .NET 10 Hosting Bundle

Download and install the .NET 10 Hosting Bundle:

```
https://dotnet.microsoft.com/download/dotnet/10.0
```

The Hosting Bundle includes:
- .NET Runtime
- ASP.NET Core Runtime
- ASP.NET Core Module (ANCM) for IIS

**Verify Installation:**
```cmd
dotnet --list-runtimes
```

### 2. WebSocket Protocol

The MCP HTTP transport uses Server-Sent Events (SSE), which requires WebSocket support:

1. Open **Server Manager**
2. Click **Add roles and features**
3. Navigate to **Server Roles** → **Web Server (IIS)** → **Web Server** → **Application Development**
4. Check **WebSocket Protocol**
5. Complete the installation

---

## Publishing the Application

### Option 1: Command Line (Recommended)

```bash
cd d:\projects\Claude

# Publish release build
dotnet publish McpCalculator.Web -c Release -o ./publish/web
```

### Option 2: Visual Studio

1. Right-click **McpCalculator.Web** project
2. Select **Publish**
3. Choose **Folder** target
4. Set path to `./publish/web`
5. Click **Publish**

### Published Files

The `publish/web` folder should contain:

```
publish/web/
├── McpCalculator.Web.dll          # Main application
├── McpCalculator.Web.exe          # Executable (optional)
├── McpCalculator.Core.dll         # Core library
├── appsettings.json               # Configuration
├── appsettings.Development.json   # Dev config (remove for production)
├── web.config                     # IIS configuration
└── [other dependencies]
```

---

## IIS Configuration

### Step 1: Create Application Pool

1. Open **IIS Manager**
2. Right-click **Application Pools** → **Add Application Pool**
3. Configure:
   - **Name:** `McpCalculatorPool`
   - **.NET CLR Version:** `No Managed Code`
   - **Managed Pipeline Mode:** `Integrated`
4. Click **OK**

**Configure Pool Settings:**
1. Right-click the new pool → **Advanced Settings**
2. Set **Start Mode:** `AlwaysRunning` (optional, for faster first request)
3. Set **Idle Time-out:** `0` (prevents app from stopping when idle)

### Step 2: Create Website or Application

**Option A: New Website**

1. Right-click **Sites** → **Add Website**
2. Configure:
   - **Site name:** `McpCalculator`
   - **Application pool:** `McpCalculatorPool`
   - **Physical path:** `C:\inetpub\McpCalculator` (or your publish folder)
   - **Binding:** Type `https`, Port `443`, select SSL certificate
3. Click **OK**

**Option B: Application Under Existing Site**

1. Right-click your existing site → **Add Application**
2. Configure:
   - **Alias:** `mcp-calculator`
   - **Application pool:** `McpCalculatorPool`
   - **Physical path:** Path to published files
3. Click **OK**

### Step 3: Configure Authentication

Based on your chosen authentication type:

**For ApiKey or JWT:**
1. Select your site/application in IIS Manager
2. Open **Authentication**
3. Enable **Anonymous Authentication**
4. Disable **Windows Authentication**

**For Windows Authentication:**
1. Select your site/application
2. Open **Authentication**
3. Disable **Anonymous Authentication**
4. Enable **Windows Authentication**

### Step 4: Configure HTTPS (Recommended)

1. Select your site
2. Click **Bindings** in the Actions pane
3. Add HTTPS binding:
   - Type: `https`
   - Port: `443`
   - SSL certificate: Select your certificate

**Self-signed certificate (for testing):**
```powershell
New-SelfSignedCertificate -DnsName "localhost" -CertStoreLocation "cert:\LocalMachine\My"
```

---

## Configuration Files

### web.config

The `web.config` file configures the ASP.NET Core Module:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*"
             modules="AspNetCoreModuleV2" resourceType="Unspecified"/>
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\McpCalculator.Web.dll"
                  stdoutLogEnabled="false"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="InProcess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production"/>
        </environmentVariables>
      </aspNetCore>
      <webSocket enabled="true"/>
    </system.webServer>
  </location>
</configuration>
```

### appsettings.json

Configure authentication and other settings:

```json
{
  "Authentication": {
    "Type": "ApiKey",
    "ApiKey": {
      "HeaderName": "X-API-Key"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

> **Security:**
> - Store API keys in environment variables, not appsettings.json: `set Authentication__ApiKey__ValidKeys__0=your-production-api-key`
> - Remove `appsettings.Development.json` from production deployments.

---

## Hosting Models

### In-Process (Recommended)

The application runs inside the IIS worker process (w3wp.exe):

```xml
<aspNetCore hostingModel="InProcess" ... />
```

**Advantages:**
- Better performance
- Lower latency
- Simpler architecture

### Out-of-Process

The application runs in a separate Kestrel process:

```xml
<aspNetCore hostingModel="OutOfProcess" ... />
```

**Use When:**
- Debugging production issues
- Running multiple apps with different .NET versions

---

## Verification

### 1. Check Application Pool Status

```powershell
Get-WebAppPoolState -Name "McpCalculatorPool"
```

### 2. Test Health Endpoint

```powershell
Invoke-RestMethod -Uri "https://your-server/health"
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2025-01-31T10:30:00Z"
}
```

### 3. Test MCP Endpoint

```powershell
$body = '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'

Invoke-RestMethod -Uri "https://your-server/mcp" `
  -Method POST `
  -ContentType "application/json" `
  -Headers @{ "X-API-Key" = "your-api-key" } `
  -Body $body
```

### 4. Test Calculator Tool

```powershell
$body = @{
  jsonrpc = "2.0"
  id = 1
  method = "tools/call"
  params = @{
    name = "Add"
    arguments = @{ a = 5; b = 3 }
  }
} | ConvertTo-Json -Depth 5

Invoke-RestMethod -Uri "https://your-server/mcp" `
  -Method POST `
  -ContentType "application/json" `
  -Headers @{ "X-API-Key" = "your-api-key" } `
  -Body $body
```

---

## Troubleshooting

### Common Issues

#### 500.19 - Configuration Error

**Cause:** Invalid web.config or missing ASP.NET Core Module

**Solution:**
1. Verify .NET Hosting Bundle is installed
2. Check web.config syntax
3. Restart IIS: `iisreset`

#### 502.5 - Process Failure

**Cause:** Application fails to start

**Solution:**
1. Enable stdout logging in web.config:
   ```xml
   <aspNetCore stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" ... />
   ```
2. Create the `logs` folder with write permissions
3. Check the log file for errors

#### 503 - Service Unavailable

**Cause:** Application pool stopped

**Solution:**
1. Check Event Viewer for crash details
2. Verify .NET runtime version matches the application
3. Start the application pool manually

#### SSE/WebSocket Connection Drops

**Cause:** WebSocket protocol not enabled

**Solution:**
1. Verify WebSocket Protocol is installed (see Prerequisites)
2. Check web.config has `<webSocket enabled="true" />`
3. Restart IIS

### Enabling Detailed Errors

For debugging, temporarily enable detailed errors:

**web.config:**
```xml
<environmentVariables>
  <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Development"/>
</environmentVariables>
```

> **Warning:** Disable in production to avoid exposing sensitive information.

### Viewing Logs

**Application Logs:**
```powershell
Get-Content "C:\inetpub\McpCalculator\logs\stdout_*.log" -Tail 50
```

**Event Viewer:**
1. Open Event Viewer
2. Navigate to **Windows Logs** → **Application**
3. Filter by Source: `IIS AspNetCore Module V2`

---

## Security Checklist

- [ ] HTTPS enabled with valid certificate
- [ ] `ASPNETCORE_ENVIRONMENT` set to `Production`
- [ ] `appsettings.Development.json` removed
- [ ] Authentication configured (not `None`)
- [ ] API keys/secrets stored securely
- [ ] stdout logging disabled (or secured)
- [ ] Application pool has minimal permissions
- [ ] Windows Firewall allows port 443

---

## Performance Tuning

### Application Pool Settings

| Setting | Recommended Value | Description |
|---------|------------------|-------------|
| Start Mode | AlwaysRunning | Prevents cold starts |
| Idle Time-out | 0 | Keeps app running |
| Recycling | Every 29 hours | Prevents memory leaks |

### HTTP/2

Enable HTTP/2 for better performance:

1. Requires HTTPS
2. IIS 10+ enables HTTP/2 by default for HTTPS

### Response Compression

Add to `Program.cs`:

```csharp
builder.Services.AddResponseCompression();
// ...
app.UseResponseCompression();
```

---

## Updating the Application

### Zero-Downtime Deployment

1. Publish new version to a staging folder
2. Stop the application pool
3. Replace files in the application folder
4. Start the application pool

**PowerShell Script:**
```powershell
$poolName = "McpCalculatorPool"
$appPath = "C:\inetpub\McpCalculator"
$stagingPath = "C:\staging\McpCalculator"

# Stop pool
Stop-WebAppPool -Name $poolName
Start-Sleep -Seconds 5

# Replace files
Remove-Item "$appPath\*" -Recurse -Force
Copy-Item "$stagingPath\*" -Destination $appPath -Recurse

# Start pool
Start-WebAppPool -Name $poolName
```

---

## Further Reading

- [Host ASP.NET Core on Windows with IIS](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/)
- [ASP.NET Core Module Configuration](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/aspnet-core-module)
- [Troubleshoot ASP.NET Core on IIS](https://docs.microsoft.com/en-us/aspnet/core/test/troubleshoot-azure-iis)
