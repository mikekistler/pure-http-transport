using System.CommandLine;
using System.CommandLine.Parsing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PureHttpMcpClient;

var builder = Host.CreateApplicationBuilder(args);

// Add services
builder.Services.AddHttpClient<McpClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:5168/");
    client.DefaultRequestHeaders.Add("MCP-Protocol-Version", "2025-06-18");
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    // Allow self-signed certificates for development
    handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
    return handler;
});

builder.Services.AddSingleton<CliApplication>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Warning);

var host = builder.Build();

// Create root command
var rootCommand = new RootCommand("Pure HTTP MCP Client - A CLI client for Model Context Protocol servers");

// Add subcommands

var cliApp = host.Services.GetRequiredService<CliApplication>();
cliApp.ConfigureCommands(rootCommand);

// Gather top-level commands for tab completion
var commandNames = rootCommand.Children
    .OfType<Command>()
    .Select(c => c.Name)
    .Concat(new[] { "exit", "quit" })
    .ToArray();
ReadLine.AutoCompletionHandler = new SimpleAutoCompleteHandler(commandNames);

// REPL loop
while (true)
{
    var line = ReadLine.Read("mcp> ");
    if (line == null)
        break;
    var trimmed = line.Trim();
    if (string.IsNullOrEmpty(trimmed))
        continue;
    if (trimmed.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        trimmed.Equals("quit", StringComparison.OrdinalIgnoreCase))
        break;

    ReadLine.AddHistory(line);

    // Split input into args (simple split, does not handle quotes)
    var inputArgs = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    try
    {
        await rootCommand.InvokeAsync(inputArgs);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

// Tab completion handler for ReadLine
class SimpleAutoCompleteHandler : IAutoCompleteHandler
{
    private readonly string[] _commands;
    public char[] Separators { get; set; } = new[] { ' ' };

    public SimpleAutoCompleteHandler(string[] commands)
    {
        _commands = commands;
    }

    public string[] GetSuggestions(string text, int index)
    {
        if (string.IsNullOrWhiteSpace(text))
            return _commands;
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return _commands.Where(c => c.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase)).ToArray();
        }
        // Optionally, add more logic for subcommands/options
        return Array.Empty<string>();
    }
}