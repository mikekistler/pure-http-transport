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

namespace PureHttpTransport;

public static class ToolsEndpoints
{
    public static void MapToolsEndpoints(this IEndpointRouteBuilder app)
    {
        var toolsGroup = app.MapGroup("/tools")
            .WithTags("Tools");

        // "tools/list"
        toolsGroup.MapGet("/", ListTools)
            .WithName("ListTools")
            .WithSummary("Get all available tools");

        // "tools/call"
        toolsGroup.MapPost("/{name}/calls", CallTool)
            .WithName("CallTool")
            .WithSummary("Invoke a tool call by name");
    }

    private static ListToolsResult ListTools()
    {
        var options = new JsonSerializerOptions(JsonSerializerOptions.Web){
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        var inputSchema = options.GetJsonSchemaAsNode(typeof(GetCurrentWeatherInput));
        var outputSchema = options.GetJsonSchemaAsNode(typeof(GetCurrentWeatherOutput));
        var tools = new List<Tool>
        {
            new Tool
            {
                Meta = new JsonObject (),
                Name = "getCurrentWeather",
                Title = "Get Current Weather",
                Description = "Get the current weather in a given location",
                InputSchema = inputSchema != null ? JsonDocument.Parse(inputSchema.ToJsonString()).RootElement : default,
                OutputSchema = outputSchema != null ? JsonDocument.Parse(outputSchema.ToJsonString()).RootElement : default,
                Annotations = new ToolAnnotations
                {
                    Title = "Get Current Weather Tool",
                    ReadOnlyHint = true,
                    OpenWorldHint = true
                }
            }
        };

        var result = new ListToolsResult
        {
            Meta = new JsonObject
            {
                ["totalTools"] = tools.Count
            },
            NextCursor = string.Empty,
            Tools = tools
        };

        return result;
    }

    private static Results<Ok<CallToolResult>, BadRequest<ProblemDetails>> CallTool(
        [Description("The name of the tool to call")] string name,

        [FromBody] CallToolRequestParams requestParams,

        [Description("The unique request ID for tracking purposes")]
        [FromHeader(Name = "Mcp-Request-Id")] string? mcpRequestId
    )
    {
        // Basic validation
        if (string.IsNullOrEmpty(name))
        {
            return TypedResults.BadRequest<ProblemDetails>(new()
            {
                Detail = "tool name required"
            });
        }

        // Dispatch to supported tools
        switch (name)
        {
            case "getCurrentWeather":
                return TypedResults.Ok<CallToolResult>(HandleGetCurrentWeather(requestParams.Arguments));
            default:
                return TypedResults.Ok<CallToolResult>(new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock> { new TextContentBlock { Text = $"unknown tool: {name}" } }
                });
        }
    }

    private static CallToolResult HandleGetCurrentWeather(IReadOnlyDictionary<string, JsonElement>? arguments)
    {
        string location = "Unknown";
        string unit = "celsius";

        if (arguments != null)
        {
            if (arguments.TryGetValue("location", out var locationElem) && locationElem.ValueKind == JsonValueKind.String)
            {
                location = locationElem.GetString() ?? "Unknown";
            }
            if (arguments.TryGetValue("unit", out var unitElem) && unitElem.ValueKind == JsonValueKind.String)
            {
                unit = unitElem.GetString() ?? "celsius";
            }
        }

        // Fake weather for demonstration
        var temp = unit == "fahrenheit" ? 68.0 : 20.0;

        var structuredNode = new JsonObject
        {
            ["temperature"] = temp,
            ["unit"] = unit,
            ["description"] = $"Clear skies in {location}"
        };

        return new CallToolResult
        {
            Content = new List<ContentBlock> { new TextContentBlock { Text = (string)structuredNode["description"]! } as ContentBlock },
            StructuredContent = structuredNode,
            IsError = false
        };
    }
}

// --- POCO for schema generation ---

public class GetCurrentWeatherInput
{
    /// <summary>The city and state, e.g. San Francisco, CA</summary>
    public string Location { get; set; } = string.Empty;
    /// <summary>The unit of temperature, either 'celsius' or 'fahrenheit'</summary>
    public string? Unit { get; set; }
}

public class GetCurrentWeatherOutput
{
    /// <summary>The current temperature</summary>
    public double Temperature { get; set; }
    /// <summary>The unit of temperature, either 'celsius' or 'fahrenheit'</summary>
    public string Unit { get; set; } = string.Empty;
    /// <summary>A brief description of the current weather</summary>
    public string Description { get; set; } = string.Empty;
}