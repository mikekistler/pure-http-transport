using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using PureHttpTransport.Models;
using System.Collections.Generic;

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
        toolsGroup.MapPut("/{name}/calls/{callId}", CallTool)
            .WithName("CallTool")
            .WithSummary("Invoke a tool call by name");
    }

    private static ListToolsResult ListTools()
    {
        var tools = new List<Tool>
        {
            new Tool
            {
                _meta = new Dictionary<string, object>(),
                Name = "getCurrentWeather",
                Title = "Get Current Weather",
                Description = "Get the current weather in a given location",
                InputSchema = new Schema
                {
                    Type = "object",
                    Properties = new Dictionary<string, object>
                    {
                        { "location", new Schema { Type = "string", Description = "The city and state, e.g. San Francisco, CA", Properties = new Dictionary<string, object>(), Required = new List<string>() } },
                        { "unit", new Schema { Type = "string", Description = "The unit of temperature, either 'celsius' or 'fahrenheit'", Properties = new Dictionary<string, object>(), Required = new List<string>() } }
                    },
                    Required = new List<string> { "location" }
                },
                OutputSchema = new Schema
                {
                    Type = "object",
                    Properties = new Dictionary<string, object>
                    {
                        { "temperature", new Schema { Type = "number", Description = "The current temperature", Properties = new Dictionary<string, object>(), Required = new List<string>() } },
                        { "unit", new Schema { Type = "string", Description = "The unit of temperature, either 'celsius' or 'fahrenheit'", Properties = new Dictionary<string, object>(), Required = new List<string>() } },
                        { "description", new Schema { Type = "string", Description = "A brief description of the current weather", Properties = new Dictionary<string, object>(), Required = new List<string>() } }
                    },
                    Required = new List<string> { "temperature", "unit", "description" }
                },
                Annotations = new ToolAnnotations
                {
                    Title = "Get Current Weather Tool",
                    ReadOnlyHint = true,
                    OpenWorldHint = true
                }
            },

            new Tool
            {
                _meta = new Dictionary<string, object>(),
                Name = "getWeatherForecast",
                Title = "Get Weather Forecast",
                Description = "Get the weather forecast for the next 5 days in a given location",
                InputSchema = new Schema
                {
                    Type = "object",
                    Properties = new Dictionary<string, object>
                    {
                        { "location", new Schema { Type = "string", Description = "The city and state, e.g. San Francisco, CA", Properties = new Dictionary<string, object>(), Required = new List<string>() } },
                        { "unit", new Schema { Type = "string", Description = "The unit of temperature, either 'celsius' or 'fahrenheit'", Properties = new Dictionary<string, object>(), Required = new List<string>() } }
                    },
                    Required = new List<string> { "location" }
                },
                OutputSchema = new Schema
                {
                    Type = "array",
                    Properties = new Dictionary<string, object>
                    {
                        { "items", new Schema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, object>
                                {
                                    { "date", new Schema { Type = "string", Description = "Date of the forecast in YYYY-MM-DD", Properties = new Dictionary<string, object>(), Required = new List<string>() } },
                                    { "high", new Schema { Type = "number", Description = "Expected high temperature", Properties = new Dictionary<string, object>(), Required = new List<string>() } },
                                    { "low", new Schema { Type = "number", Description = "Expected low temperature", Properties = new Dictionary<string, object>(), Required = new List<string>() } },
                                    { "unit", new Schema { Type = "string", Description = "Unit of temperature", Properties = new Dictionary<string, object>(), Required = new List<string>() } },
                                    { "description", new Schema { Type = "string", Description = "Short forecast description", Properties = new Dictionary<string, object>(), Required = new List<string>() } }
                                },
                                Required = new List<string> { "date", "high", "low", "unit", "description" }
                            }
                        }
                    }
                },
                Annotations = new ToolAnnotations
                {
                    Title = "5-day Weather Forecast Tool",
                    ReadOnlyHint = true,
                    OpenWorldHint = true
                }
            }
        };

        var result = new ListToolsResult
        {
            _meta = new Dictionary<string, object>
            {
                { "totalTools", tools.Count }
            },
            NextCursor = string.Empty,
            Tools = tools
        };

        return result;
    }

    private static CallToolResult CallTool(string name, string callId, [FromBody] Dictionary<string, object>? arguments)
    {
        // Basic validation
        if (string.IsNullOrEmpty(name))
        {
            return new CallToolResult
            {
                isError = true,
                content = new List<object> { new { text = "tool name required" } }
            };
        }

        // Dispatch to supported tools
        switch (name)
        {
            case "getCurrentWeather":
                return HandleGetCurrentWeather(arguments);
            case "getWeatherForecast":
                return HandleGetWeatherForecast(arguments);
            default:
                return new CallToolResult
                {
                    isError = true,
                    content = new List<object> { new { text = $"unknown tool: {name}" } }
                };
        }
    }

    private static CallToolResult HandleGetCurrentWeather(Dictionary<string, object>? arguments)
    {
        arguments ??= new Dictionary<string, object>();
        arguments.TryGetValue("location", out var locationObj);
        var location = locationObj as string ?? "Unknown";

        arguments.TryGetValue("unit", out var unitObj);
        var unit = (unitObj as string) ?? "celsius";

        // Fake weather for demonstration
        var temp = unit == "fahrenheit" ? 68.0 : 20.0;

        var structured = new Dictionary<string, object>
        {
            { "temperature", temp },
            { "unit", unit },
            { "description", $"Clear skies in {location}" }
        };

        return new CallToolResult
        {
            content = new List<object> { new { text = structured["description"] } },
            structuredContent = structured,
            isError = false
        };
    }

    private static CallToolResult HandleGetWeatherForecast(Dictionary<string, object>? arguments)
    {
        arguments ??= new Dictionary<string, object>();
        arguments.TryGetValue("location", out var locationObj);
        var location = locationObj as string ?? "Unknown";

        arguments.TryGetValue("unit", out var unitObj);
        var unit = (unitObj as string) ?? "celsius";

        // Create a 5-day fake forecast
        var items = new List<Dictionary<string, object>>();
        for (int i = 0; i < 5; i++)
        {
            items.Add(new Dictionary<string, object>
            {
                { "date", System.DateTime.UtcNow.AddDays(i).ToString("yyyy-MM-dd") },
                { "high", unit == "fahrenheit" ? 75 + i : 24 + i },
                { "low", unit == "fahrenheit" ? 55 + i : 13 + i },
                { "unit", unit },
                { "description", $"Day {i+1}: Sunny in {location}" }
            });
        }

        var structured = new Dictionary<string, object>
        {
            { "forecast", items }
        };

        return new CallToolResult
        {
            content = new List<object> { new { text = $"Returning {items.Count}-day forecast for {location}" } },
            structuredContent = structured,
            isError = false
        };
    }
}
