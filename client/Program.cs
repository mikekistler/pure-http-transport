using System.CommandLine;
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

// Run the command line interface
return await rootCommand.InvokeAsync(args);