using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using PureHttpTransport.Models;
using System;
using System.Collections.Generic;

namespace PureHttpTransport;

public static class InitializeEndpoints
{
    public static IEndpointRouteBuilder MapInitializeEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/initialize", (InitializeRequestParams? initParams, HttpResponse response) =>
        {
            // Create a session id for the client
            var sessionId = Guid.NewGuid().ToString();
            response.Headers["Mcp-Session-Id"] = sessionId;
            // Advertise the protocol version we support
            response.Headers["MCP-Protocol-Version"] = "2025-06-18";

            var result = new InitializeResult
            {
                _meta = new Dictionary<string, object>(),
                Capabilities = new Dictionary<string, object>
                {
                    { "tools", new Dictionary<string, object>() },
                    { "prompts", new Dictionary<string, object> { { "listChanged", false } } },
                    { "resources", new Dictionary<string, object> { { "listChanged", false }, { "subscribe", false } } }
                },
                ProtocolVersion = "2025-06-18",
                ServerInfo = new Implementation
                {
                    Name = "PureHttpMcpServer",
                    Version = "0.1.0",
                    Title = "Pure HTTP MCP Server"
                },
                Instructions = "This is a test Pure HTTP MCP server implementation."
            };

            return Results.Json(result);
        })
        .WithName("Initialize")
        .WithSummary("Initialize the MCP session and return server capabilities");

        return app;
    }
}
