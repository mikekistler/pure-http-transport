using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;

namespace PureHttpTransport;

public static class InitializeEndpoints
{
    public static IEndpointRouteBuilder MapInitializeEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/initialize", (InitializeRequestParams? initParams, HttpResponse response) =>
        {
            // Advertise the protocol version we support
            response.Headers["MCP-Protocol-Version"] = "2025-06-18";

            InitializeResult result = new InitializeResult
            {
                Meta = new JsonObject
                {
                    ["exampleKey"] = "exampleValue"
                },
                Capabilities = JsonSerializer.Deserialize<ServerCapabilities>(
                    JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        { "tools", new Dictionary<string, object>() },
                        { "prompts", new Dictionary<string, object> { { "listChanged", false } } },
                        { "resources", new Dictionary<string, object> { { "listChanged", false }, { "subscribe", false } } }
                    })
                ) ?? new ServerCapabilities(),
                ProtocolVersion = "2025-06-18",
                ServerInfo = new Implementation
                {
                    Name = "PureHttpMcpServer",
                    Version = "0.1.0",
                    Title = "Pure HTTP MCP Server"
                },
                Instructions = "This is a test Pure HTTP MCP server implementation."
            };

            return TypedResults.Ok<InitializeResult>(result);
        })
        .WithName("Initialize")
        .WithSummary("Initialize the MCP session and return server capabilities");

        return app;
    }
}
