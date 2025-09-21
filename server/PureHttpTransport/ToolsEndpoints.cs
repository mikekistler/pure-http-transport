using Microsoft.AspNetCore.Mvc;
using PureHttpTransport.Models;

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
    }

    private static ListToolsResult ListTools()
    {
        var result = new ListToolsResult
        {
            _meta = new Dictionary<string, object>
            {
                { "totalTools", 2 }
            },
            NextCursor = string.Empty,
            Tools = new List<Tool>
            {
                new Tool
                {
                    Name = "getCurrentWeather",
                    Title = "Get Current Weather",
                    Description = "Get the current weather in a given location",
                    InputSchema = new Schema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, object>
                        {
                            { "location", new Schema { Type = "string", Description = "The city and state, e.g. San Francisco, CA" } },
                            { "unit", new Schema { Type = "string", Description = "The unit of temperature, either 'celsius' or 'fahrenheit'" } }
                        },
                        Required = new List<string> { "location" }
                    },
                    OutputSchema = new Schema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, object>
                        {
                            { "temperature", new Schema { Type = "number", Description = "The current temperature" } },
                            { "unit", new Schema { Type = "string", Description = "The unit of temperature, either 'celsius' or 'fahrenheit'" } },
                            { "description", new Schema { Type = "string", Description = "A brief description of the current weather" } }
                        },
                        Required = new List<string> { "temperature", "unit", "description" }
                    },
                    Annotations = new ToolAnnotations
                    {
                        Title = "Get Current Weather Tool"
                    }
                },
                new Tool
                {
                    Name = "getWeatherForecast",
                    Title = "Get Weather Forecast",
                    Description = "Get the weather forecast for the next 5 days in a given location",
                    InputSchema = new Schema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, object>
                        {
                            { "location", new Schema { Type = "string", Description = "The city and state, e.g. San Francisco, CA" } },
                            { "unit", new Schema { Type = "string", Description = "The unit of temperature, either 'celsius' or 'fahrenheit'" } }
                        },
                        Required = new List<string> { "location" }
                    },
                    OutputSchema = new Schema
                    {
                }
            }
            }
        };
        return result;
    }
}
