using ModelContextProtocol.Protocol;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;

namespace PureHttpMcpServer.Tools;

public static class MockTools
{
    public static List<Tool> ListTools()
    {
        var inputSchema = GetSchema(typeof(GetCurrentWeatherInput));
        var outputSchema = GetSchema(typeof(GetCurrentWeatherOutput));
        var result = new List<Tool>
        {
            new Tool
            {
                Meta = new JsonObject (),
                Name = "getCurrentWeather",
                Title = "Get Current Weather",
                Description = "Get the current weather in a given location",
                InputSchema = inputSchema ?? default,
                OutputSchema = outputSchema ?? default,
                Annotations = new ToolAnnotations
                {
                    Title = "Get Current Weather Tool",
                    ReadOnlyHint = true,
                    OpenWorldHint = true
                }
            }
        };

        return result;
    }

    static JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerOptions.Web)
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
    static JsonSchemaExporterOptions exporterOptions = new JsonSchemaExporterOptions
    {
        TreatNullObliviousAsNonNullable = true
    };
    private static JsonElement? GetSchema(Type type)
    {
        var schemaAsNode = JsonSchemaExporter.GetJsonSchemaAsNode(options, type, exporterOptions);
        if (schemaAsNode == null) return null;
        var schemaAsElement = JsonDocument.Parse(schemaAsNode.ToJsonString()).RootElement;
        return schemaAsElement;
    }
    public static CallToolResult CallTool(string name, CallToolRequestParams requestParams)
    {
        // Dispatch to supported tools
        switch (name)
        {
            case "getCurrentWeather":
                return HandleGetCurrentWeather(requestParams.Arguments);
            default:
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock> { new TextContentBlock { Text = $"unknown tool: {name}" } }
                };
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