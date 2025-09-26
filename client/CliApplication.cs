using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace PureHttpMcpClient;

public class CliApplication
{
    private readonly McpClient _mcpClient;
    private readonly ILogger<CliApplication> _logger;

    public CliApplication(McpClient mcpClient, ILogger<CliApplication> logger)
    {
        _mcpClient = mcpClient;
        _logger = logger;
    }

    public void ConfigureCommands(RootCommand rootCommand)
    {
        // Initialize command
        var initCommand = new Command("init", "Initialize connection to MCP server");
        initCommand.SetHandler(InitializeCommandHandler);
        rootCommand.Add(initCommand);

        // Ping command
        var pingCommand = new Command("ping", "Send ping to MCP server");
        pingCommand.SetHandler(PingCommandHandler);
        rootCommand.Add(pingCommand);

        // Tools commands
        var toolsCommand = new Command("tools", "Work with MCP tools");

        var listToolsCommand = new Command("list", "List available tools");
        var cursorOption = new Option<string?>("--cursor", "Pagination cursor");
        listToolsCommand.AddOption(cursorOption);
        listToolsCommand.SetHandler(ListToolsCommandHandler, cursorOption);
        toolsCommand.Add(listToolsCommand);

        var callToolCommand = new Command("call", "Call a tool");
        var toolNameArg = new Argument<string>("name", "Tool name to call");
        var argumentsOption = new Option<string?>("--args", "Tool arguments as JSON");
        callToolCommand.AddArgument(toolNameArg);
        callToolCommand.AddOption(argumentsOption);
        callToolCommand.SetHandler(CallToolCommandHandler, toolNameArg, argumentsOption);
        toolsCommand.Add(callToolCommand);

        rootCommand.Add(toolsCommand);

        // Resources commands
        var resourcesCommand = new Command("resources", "Work with MCP resources");

        var listResourcesCommand = new Command("list", "List available resources");
        var resourceCursorOption = new Option<string?>("--cursor", "Pagination cursor");
        listResourcesCommand.AddOption(resourceCursorOption);
        listResourcesCommand.SetHandler(ListResourcesCommandHandler, resourceCursorOption);
        resourcesCommand.Add(listResourcesCommand);

        var readResourceCommand = new Command("read", "Read a resource");
        var resourceUriArg = new Argument<string>("uri", "Resource URI to read");
        readResourceCommand.AddArgument(resourceUriArg);
        readResourceCommand.SetHandler(ReadResourceCommandHandler, resourceUriArg);
        resourcesCommand.Add(readResourceCommand);

        rootCommand.Add(resourcesCommand);

        // Prompts commands
        var promptsCommand = new Command("prompts", "Work with MCP prompts");

        var listPromptsCommand = new Command("list", "List available prompts");
        var promptCursorOption = new Option<string?>("--cursor", "Pagination cursor");
        listPromptsCommand.AddOption(promptCursorOption);
        listPromptsCommand.SetHandler(ListPromptsCommandHandler, promptCursorOption);
        promptsCommand.Add(listPromptsCommand);

        var getPromptCommand = new Command("get", "Get a prompt");
        var promptNameArg = new Argument<string>("name", "Prompt name to get");
        var promptArgumentsOption = new Option<string?>("--args", "Prompt arguments as JSON");
        getPromptCommand.AddArgument(promptNameArg);
        getPromptCommand.AddOption(promptArgumentsOption);
        getPromptCommand.SetHandler(GetPromptCommandHandler, promptNameArg, promptArgumentsOption);
        promptsCommand.Add(getPromptCommand);

        rootCommand.Add(promptsCommand);
    }

    private async Task InitializeCommandHandler()
    {
        try
        {
            Console.WriteLine("Initializing MCP client...");
            var result = await _mcpClient.InitializeAsync();
            if (result != null)
            {
                Console.WriteLine("✅ Successfully initialized MCP client");
                Console.WriteLine($"Server: {result.ServerInfo?.Name} v{result.ServerInfo?.Version}");
                Console.WriteLine($"Protocol version: {result.ProtocolVersion}");
                if (!string.IsNullOrEmpty(result.Instructions))
                {
                    Console.WriteLine($"Instructions: {result.Instructions}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to initialize: {ex.Message}");
        }
    }

    private async Task PingCommandHandler()
    {
        try
        {
            Console.WriteLine("Pinging MCP server...");
            var success = await _mcpClient.PingAsync();
            if (success)
            {
                Console.WriteLine("✅ Ping successful");
            }
            else
            {
                Console.WriteLine("❌ Ping failed");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ping failed: {ex.Message}");
        }
    }

    private async Task ListToolsCommandHandler(string? cursor)
    {
        try
        {
            Console.WriteLine("Listing available tools...");
            var tools = await _mcpClient.ListToolsAsync(cursor);
            if (tools != null && tools.Count > 0)
            {
                Console.WriteLine($"Found {tools.Count} tool(s):");
                foreach (var tool in tools)
                {
                    Console.WriteLine($"  📧 {tool.Name}");
                    if (!string.IsNullOrEmpty(tool.Title))
                    {
                        Console.WriteLine($"     Title: {tool.Title}");
                    }
                    if (!string.IsNullOrEmpty(tool.Description))
                    {
                        Console.WriteLine($"     Description: {tool.Description}");
                    }
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("No tools available");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to list tools: {ex.Message}");
        }
    }

    private async Task CallToolCommandHandler(string name, string? argumentsJson)
    {
        try
        {
            Dictionary<string, JsonElement>? arguments = null;
            if (!string.IsNullOrEmpty(argumentsJson))
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
                arguments = parsed;
            }

            Console.WriteLine($"Calling tool '{name}'...");
            var result = await _mcpClient.CallToolAsync(name, arguments);
            if (result != null)
            {
                if (result.IsError == true)
                {
                    Console.WriteLine("❌ Tool call failed:");
                }
                else
                {
                    Console.WriteLine("✅ Tool call successful:");
                }

                if (result.Content != null)
                {
                    foreach (var content in result.Content)
                    {
                        if (content is TextContentBlock textContent)
                        {
                            Console.WriteLine(textContent.Text);
                        }
                    }
                }

                if (result.StructuredContent != null)
                {
                    Console.WriteLine("\nStructured content:");
                    Console.WriteLine(result.StructuredContent.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to call tool: {ex.Message}");
        }
    }

    private async Task ListResourcesCommandHandler(string? cursor)
    {
        try
        {
            Console.WriteLine("Listing available resources...");
            var resources = await _mcpClient.ListResourcesAsync(cursor);
            if (resources != null && resources.Count > 0)
            {
                Console.WriteLine($"Found {resources.Count} resource(s):");
                foreach (var resource in resources)
                {
                    Console.WriteLine($"  📄 {resource.Uri}");
                    if (!string.IsNullOrEmpty(resource.Name))
                    {
                        Console.WriteLine($"     Name: {resource.Name}");
                    }
                    if (!string.IsNullOrEmpty(resource.Description))
                    {
                        Console.WriteLine($"     Description: {resource.Description}");
                    }
                    if (!string.IsNullOrEmpty(resource.MimeType))
                    {
                        Console.WriteLine($"     MIME Type: {resource.MimeType}");
                    }
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("No resources available");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to list resources: {ex.Message}");
        }
    }

    private async Task ReadResourceCommandHandler(string uri)
    {
        try
        {
            Console.WriteLine($"Reading resource '{uri}'...");
            var result = await _mcpClient.ReadResourceAsync(uri);
            if (result != null && result.Contents != null)
            {
                Console.WriteLine("✅ Resource content:");
                foreach (var content in result.Contents)
                {
                    if (content is TextResourceContents textContent)
                    {
                        Console.WriteLine($"Text content (URI: {textContent.Uri}):");
                        Console.WriteLine(textContent.Text);
                    }
                    else if (content is BlobResourceContents blobContent)
                    {
                        Console.WriteLine($"Binary content (URI: {blobContent.Uri}, MIME: {blobContent.MimeType})");
                        Console.WriteLine($"Size: {Convert.FromBase64String(blobContent.Blob).Length} bytes");
                    }
                }
            }
            else
            {
                Console.WriteLine("No content found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to read resource: {ex.Message}");
        }
    }

    private async Task ListPromptsCommandHandler(string? cursor)
    {
        try
        {
            Console.WriteLine("Listing available prompts...");
            var prompts = await _mcpClient.ListPromptsAsync(cursor);
            if (prompts != null && prompts.Count > 0)
            {
                Console.WriteLine($"Found {prompts.Count} prompt(s):");
                foreach (var prompt in prompts)
                {
                    Console.WriteLine($"  💬 {prompt.Name}");
                    if (!string.IsNullOrEmpty(prompt.Title))
                    {
                        Console.WriteLine($"     Title: {prompt.Title}");
                    }
                    if (!string.IsNullOrEmpty(prompt.Description))
                    {
                        Console.WriteLine($"     Description: {prompt.Description}");
                    }
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("No prompts available");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to list prompts: {ex.Message}");
        }
    }

    private async Task GetPromptCommandHandler(string name, string? argumentsJson)
    {
        try
        {
            Dictionary<string, JsonElement>? arguments = null;
            if (!string.IsNullOrEmpty(argumentsJson))
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);
                arguments = parsed;
            }

            Console.WriteLine($"Getting prompt '{name}'...");
            var result = await _mcpClient.GetPromptAsync(name, arguments);
            if (result != null)
            {
                Console.WriteLine("✅ Prompt retrieved:");
                if (!string.IsNullOrEmpty(result.Description))
                {
                    Console.WriteLine($"Description: {result.Description}");
                }

                if (result.Messages != null && result.Messages.Count > 0)
                {
                    Console.WriteLine("\nMessages:");
                    foreach (var message in result.Messages)
                    {
                        Console.WriteLine($"  Role: {message.Role}");
                        if (message.Content is TextContentBlock textContent)
                        {
                            Console.WriteLine($"  Content: {textContent.Text}");
                        }
                        Console.WriteLine();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to get prompt: {ex.Message}");
        }
    }
}