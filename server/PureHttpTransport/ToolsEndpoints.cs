using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using ModelContextProtocol.Protocol;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Schema;
using System.ComponentModel;
using Microsoft.AspNetCore.Http.HttpResults;
using PureHttpMcpServer.Tools;

namespace PureHttpTransport;

public static class ToolsEndpoints
{
    public static void MapToolsEndpoints(this IEndpointRouteBuilder app)
    {
        var toolsGroup = app.MapGroup("/tools").WithTags("Tools");
        toolsGroup.AddEndpointFilter<ProtocolVersionFilter>();

        // "tools/list"
        toolsGroup.MapGet("/", Ok<ListToolsResult> (
            [Description("An opaque token representing the current pagination position. If provided, the server should return results starting after this cursor.")]
            string? cursor
        ) =>
        {
            var tools = MockTools.ListTools();

            var result = new ListToolsResult
            {
                Meta = new JsonObject
                {
                    ["totalTools"] = tools.Count
                },
                NextCursor = string.Empty,
                Tools = tools
            };

            return TypedResults.Ok(result);
        })
        .WithName("ListTools")
        .WithDescription("Get all available tools");

        // "tools/call"
        toolsGroup.MapPost("/{name}/calls", Results<Ok<CallToolResult>, BadRequest<ProblemDetails>> (
            [Description("The name of the tool to call")] string name,

            [FromBody] CallToolRequestParams requestParams
        ) =>
        {
            // Basic validation
            if (string.IsNullOrEmpty(name))
            {
                return TypedResults.BadRequest<ProblemDetails>(new()
                {
                    Detail = "tool name required"
                });
            }
            var result = MockTools.CallTool(name, requestParams);

            return TypedResults.Ok<CallToolResult>(result);
        })
        .WithName("CallTool")
        .WithDescription("Invoke a tool call by name");
    }
}