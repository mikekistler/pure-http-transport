using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using ModelContextProtocol.Protocol;

namespace PureHttpTransport;

public static class InitializeEndpoints
{
    public static IEndpointRouteBuilder MapInitializeEndpoint(this IEndpointRouteBuilder app)
    {
        var initialize = app.MapGroup("/initialize").WithTags("Initialization");

        initialize.MapPost("/", Results<Ok<InitializeResult>, BadRequest<ProblemDetails>> (
            InitializeRequestParams? initParams
        ) =>
        {
            if (initParams == null)
            {
                return TypedResults.BadRequest<ProblemDetails>(new()
                {
                    Detail = "Request body is required.",
                });
            }

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
