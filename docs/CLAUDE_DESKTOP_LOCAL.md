# Claude Desktop Configuration - Local MCP Server (stdio)

This guide explains how to configure Claude Desktop to use the MCP BasicCalculator server locally via stdio transport.

## Overview

The **local** configuration runs the MCP server as a child process of Claude Desktop. Communication happens through stdin/stdout using JSON-RPC 2.0.

```
┌─────────────────┐         stdio          ┌─────────────────┐
│  Claude Desktop │ ◄──────────────────────► │  McpCalculator  │
│                 │   (stdin/stdout)        │  (Console App)  │
└─────────────────┘                         └─────────────────┘
```

**Advantages:**
- No network configuration required
- No authentication needed (process-level security)
- Simple setup
- Works offline

**Disadvantages:**
- Only works on the local machine
- One server instance per Claude Desktop session

## Prerequisites

1. **.NET 10 SDK** installed
2. **McpCalculator** project built

### Build the Project

```bash
cd McpCalculator
dotnet build -c Release
```

The executable will be at:
- **Windows**: `McpCalculator\bin\Release\net10.0\McpCalculator.exe`
- **macOS/Linux**: `McpCalculator/bin/Release/net10.0/McpCalculator`

## Configuration

### Step 1: Locate Claude Desktop Config File

The configuration file location depends on your operating system:

| OS | Config File Path |
|----|------------------|
| **Windows** | `%APPDATA%\Claude\claude_desktop_config.json` |
| **macOS** | `~/Library/Application Support/Claude/claude_desktop_config.json` |
| **Linux** | `~/.config/Claude/claude_desktop_config.json` |

### Step 2: Add MCP Server Configuration

Open `claude_desktop_config.json` and add the `mcpServers` section:

#### Windows Example

```json
{
  "mcpServers": {
    "BasicCalculator": {
      "command": "D:\\projects\\Claude\\McpCalculator\\McpCalculator\\bin\\Release\\net10.0\\win-x64\\McpCalculator.exe",
      "args": []
    }
  }
}
```

#### macOS/Linux Example

```json
{
  "mcpServers": {
    "BasicCalculator": {
      "command": "/path/to/McpCalculator/McpCalculator/bin/Release/net10.0/McpCalculator",
      "args": []
    }
  }
}
```

#### Using dotnet run (Development)

If you prefer to run from source during development:

```json
{
  "mcpServers": {
    "BasicCalculator": {
      "command": "dotnet",
      "args": ["run", "--project", "D:\\projects\\Claude\\McpCalculator\\McpCalculator\\McpCalculator.csproj", "--no-build"],
      "env": {}
    }
  }
}
```

> **Note:** Using `--no-build` speeds up startup but requires a prior `dotnet build`.

### Step 3: Restart Claude Desktop

After saving the configuration file, restart Claude Desktop for changes to take effect.

> **Note:** Restarting Claude Desktop requires right-clicking icon in the task bar and selecting Quit. Simply closing the window does not restart Claude desktop.

## Configuration Options

### Full Configuration Schema

```json
{
  "mcpServers": {
    "BasicCalculator": {
      "command": "/path/to/McpCalculator.exe",
      "args": [],
      "env": {
        "DOTNET_ENVIRONMENT": "Production"
      },
      "cwd": "/path/to/working/directory"
    }
  }
}
```

| Property | Description | Required |
|----------|-------------|----------|
| `command` | Path to the executable or command to run | Yes |
| `args` | Array of command-line arguments | No |
| `env` | Environment variables to set | No |
| `cwd` | Working directory for the process | No |

### Multiple MCP Servers

You can configure multiple MCP servers:

```json
{
  "mcpServers": {
    "BasicCalculator": {
      "command": "D:\\projects\\McpCalculator\\McpCalculator.exe",
      "args": []
    },
    "weather": {
      "command": "D:\\projects\\McpWeather\\McpWeather.exe",
      "args": []
    }
  }
}
```

## Available Tools

Once configured, the following BasicCalculator tools become available in Claude Desktop:

| Tool | Description | Parameters |
|------|-------------|------------|
| `Add` | Adds two numbers | `a` (number), `b` (number) |
| `Subtract` | Subtracts b from a | `a` (number), `b` (number) |
| `Multiply` | Multiplies two numbers | `a` (number), `b` (number) |
| `Divide` | Divides a by b | `a` (number), `b` (number) |

### Usage Examples in Claude

Once configured, you can ask Claude:

- "What is 42 plus 17?"
- "Calculate 100 divided by 7"
- "Multiply 3.14 by 2"

Claude will automatically use the BasicCalculator tools to provide accurate results.

## Troubleshooting

### Server Not Appearing in Claude

1. **Check the config file path** - Ensure you're editing the correct file
2. **Validate JSON syntax** - Use a JSON validator
3. **Check executable path** - Ensure the path is correct and the file exists
4. **Restart Claude Desktop** - Changes require a restart

### Verify the Executable Works

Test the executable directly:

```bash
# Should start and wait for JSON-RPC input
./McpCalculator.exe
```

Press `Ctrl+C` to exit.

### Check Claude Desktop Logs

Claude Desktop logs MCP server issues:

| OS | Log Location |
|----|--------------|
| **Windows** | `%APPDATA%\Claude\logs\` |
| **macOS** | `~/Library/Logs/Claude/` |
| **Linux** | `~/.local/share/Claude/logs/` |

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| "Server not found" | Wrong executable path | Verify the path exists |
| "Failed to start" | Missing .NET runtime | Install .NET 10 SDK |
| "Connection lost" | Server crashed | Check server logs |
| "Tool not found" | Server not initialized | Restart Claude Desktop |

## Publishing for Distribution

To create a self-contained executable that doesn't require .NET to be installed:

```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true

# macOS x64
dotnet publish -c Release -r osx-x64 --self-contained true

# macOS ARM (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained true

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true
```

The self-contained executable will be in:
```
McpCalculator/bin/Release/net10.0/{runtime}/publish/McpCalculator.exe
```

## Security Considerations

- The stdio transport relies on process-level security
- The server runs with the same permissions as Claude Desktop
- No authentication is needed (the process is local and trusted)
- Logging is disabled to prevent stdout corruption

## Related Documentation

- [HTTP Transport Guide](HTTP_TRANSPORT.md) - For web deployment and custom MCP clients
- [Remote Server Guide](CLAUDE_DESKTOP_REMOTE.md) - For HTTP/SSE transport (custom clients, not Claude Desktop)
- [MCP Specification](https://spec.modelcontextprotocol.io/)
