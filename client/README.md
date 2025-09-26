# Pure HTTP MCP Client

A command-line client for communicating with Model Context Protocol (MCP) servers over Pure HTTP transport.

## Building and Running

### Prerequisites
- .NET 9.0 SDK

### Build the client
```bash
cd client
dotnet build
```

### Run the client
```bash
cd client
dotnet run -- <command>
```

## Available Commands

### Initialize
Initialize connection to the MCP server:
```bash
dotnet run -- init
```

### Ping
Test connectivity to the server:
```bash
dotnet run -- ping
```

### Tools
Work with MCP tools:

List available tools:
```bash
dotnet run -- tools list
dotnet run -- tools list --cursor <pagination-cursor>
```

Call a tool:
```bash
dotnet run -- tools call <tool-name>
dotnet run -- tools call <tool-name> --args '{"param": "value"}'
```

Example:
```bash
dotnet run -- tools call getCurrentWeather --args '{"location": "Seattle, WA", "unit": "fahrenheit"}'
```

### Resources
Work with MCP resources:

List available resources:
```bash
dotnet run -- resources list
dotnet run -- resources list --cursor <pagination-cursor>
```

Read a resource:
```bash
dotnet run -- resources read <resource-uri>
```

### Prompts
Work with MCP prompts:

List available prompts:
```bash
dotnet run -- prompts list
dotnet run -- prompts list --cursor <pagination-cursor>
```

Get a prompt:
```bash
dotnet run -- prompts get <prompt-name>
dotnet run -- prompts get <prompt-name> --args '{"param": "value"}'
```

## Configuration

The client is configured to connect to `https://localhost:5001` by default. You can modify the base URL in `Program.cs` if your server is running on a different address or port.

## Examples

Here's a complete workflow example:

```bash
# Start by initializing the client
dotnet run -- init

# Check server connectivity
dotnet run -- ping

# List available tools
dotnet run -- tools list

# Call the weather tool
dotnet run -- tools call getCurrentWeather --args '{"location": "New York, NY"}'

# List available resources
dotnet run -- resources list

# List available prompts
dotnet run -- prompts list
```