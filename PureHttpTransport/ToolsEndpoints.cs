using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Protocol;
using System.Text.Json.Nodes;
using System.ComponentModel;
using Microsoft.AspNetCore.Http.HttpResults;

namespace PureHttpTransport;

public static class ToolsEndpoints
{
    public static IMockTools? MockTools { get; set; } = null;

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
            var tools = MockTools?.ListTools();

            var result = new ListToolsResult
            {
                Meta = new JsonObject
                {
                    ["totalTools"] = JsonValue.Create(tools?.Count())
                },
                NextCursor = string.Empty,
                Tools = tools?.ToList() ?? new List<Tool>()
            };

            return TypedResults.Ok(result);
        })
        .WithName("ListTools")
        .WithDescription("Get all available tools");

        // "tools/call"
        toolsGroup.MapPost("/{name}/calls", async Task<Results<Ok<CallToolResult>, BadRequest<ProblemDetails>>> (
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
            if (MockTools == null)
            {
                return TypedResults.BadRequest<ProblemDetails>(new()
                {
                    Detail = "no tools available"
                });
            }
            var result = await MockTools.CallTool(name, requestParams);

            return TypedResults.Ok<CallToolResult>(result);
        })
        .WithName("CallTool")
        .WithDescription("Invoke a tool call by name");
    }
}

public interface IMockTools
{
    IEnumerable<Tool> ListTools();
    Task<CallToolResult> CallTool(string name, CallToolRequestParams requestParams);
}