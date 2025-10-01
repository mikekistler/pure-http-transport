using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Protocol;
using System.Text.Json.Nodes;
using System.ComponentModel;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Collections.Concurrent;

namespace PureHttpTransport;

public static class ToolsEndpoints
{
    public static IMockTools? MockTools { get; set; } = null;

    private class StatusMonitor(Task<CallToolResult> task)
    {
        public Task<CallToolResult> Task { get; } = task;
    }

    private static readonly ConcurrentDictionary<string, StatusMonitor> _statusMonitors = new();

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
        toolsGroup.MapPost("/{name}/calls", async Task<Results<Ok<CallToolResult>, Accepted, BadRequest<ProblemDetails>>> (
            [Description("The name of the tool to call")] string name,

            [FromBody] CallToolRequestParams requestParams,

            [Description("The unique identifier for the request")]
            [FromHeader(Name = PureHttpTransport.McpRequestIdHeader)]
            string? requestId = null
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

            var toolType = MockTools.ToolType(name);

            // If the tool is long-running, a requestId is required
            if (toolType == IMockTools.ToolTypeEnum.LongRunning && string.IsNullOrEmpty(requestId))
            {
                return TypedResults.BadRequest<ProblemDetails>(new()
                {
                    Detail = "requestId is required for long-running tools"
                });
            }
            var toolCallTask = MockTools.CallTool(name, requestParams);
            if (toolCallTask == null)
            {
                return TypedResults.BadRequest<ProblemDetails>(new()
                {
                    Detail = $"tool '{name}' not found"
                });
            }
            if (MockTools.ToolType(name) == IMockTools.ToolTypeEnum.LongRunning)
            {
                // register the task in a concurrent dictionary for status tracking
                var statusMonitor = new StatusMonitor(toolCallTask);
                _statusMonitors[requestId!] = statusMonitor;

                // return 202 Accepted with a Location header pointing to the status endpoint
                var location = $"/tools/{name}/calls/{requestId}";
                return TypedResults.Accepted(location);
            }
            var result = await toolCallTask;

            return TypedResults.Ok<CallToolResult>(result);
        })
        .WithName("CallTool")
        .WithDescription("Invoke a tool call by name");

        // "tools/call"
        toolsGroup.MapGet("/{name}/calls/{requestId}", async Task<Results<Ok<CallToolResult>, Accepted, BadRequest<ProblemDetails>>> (
            [Description("The name of the tool to call")] string name,

            [Description("The unique identifier for the request")]
            string requestId
        ) =>
        {
            if (MockTools == null)
            {
                return TypedResults.BadRequest<ProblemDetails>(new()
                {
                    Detail = "no tools available"
                });
            }

            if (!_statusMonitors.TryGetValue(requestId, out var statusMonitor))
            {
                return TypedResults.BadRequest<ProblemDetails>(new()
                {
                    Detail = $"no in-progress request found for id '{requestId}'"
                });
            }

            var toolCallTask = statusMonitor.Task;

            if (!toolCallTask.IsCompleted)
            {
                // still in progress
                var location = $"/tools/{name}/calls/{requestId}";
                return TypedResults.Accepted(location);
            }

            var result = await toolCallTask;

            // once completed, remove from the dictionary
            // Or we could keep it around for a while to allow re-querying the result?
            // _statusMonitors.TryRemove(requestId, out _);

            return TypedResults.Ok<CallToolResult>(result);
        });
    }
}

public interface IMockTools
{
    enum ToolTypeEnum
    {
        Standard,
        LongRunning,
    }

    IEnumerable<Tool> ListTools();
    Task<CallToolResult> CallTool(string name, CallToolRequestParams requestParams);

    ToolTypeEnum? ToolType(string name);
}