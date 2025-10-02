using ModelContextProtocol.Protocol;
using PureHttpTransport;
using PureHttpTransport.Models;
using System.ComponentModel;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using static ModelContextProtocol.Protocol.ElicitRequestParams;

namespace PureHttpMcpServer.Tools;

public class MockTools : IMockTools
{
    public IEnumerable<Tool> ListTools()
    {
        var result = new List<Tool>
        {
            new Tool
            {
                Meta = new JsonObject (),
                Name = "getCurrentWeather",
                Title = "Get Current Weather",
                Description = "Get the current weather in a given location",
                InputSchema = GetSchema(typeof(GetCurrentWeatherInput)) ?? default,
                OutputSchema = GetSchema(typeof(GetCurrentWeatherResult)) ?? default,
                Annotations = new ToolAnnotations
                {
                    Title = "Get Current Weather Tool",
                    ReadOnlyHint = true,
                    OpenWorldHint = true
                }
            },
            new Tool
            {
                Meta = new JsonObject (),
                Name = "GuessTheNumber",
                Description = "A simple game where the user has to guess a number between 1 and 10.",
                Annotations = new ToolAnnotations
                {
                    ReadOnlyHint = true,
                    OpenWorldHint = true,
                }
            },
            new Tool
            {
                Meta = new JsonObject (),
                Name = "RunAWhile",
                Description = "Runs for a while and then returns a string.",
                Annotations = new ToolAnnotations
                {
                    ReadOnlyHint = true,
                    OpenWorldHint = true,
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
        NumberHandling = JsonNumberHandling.Strict,
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
    public async Task<CallToolResult> CallTool(string name, CallToolRequestParams requestParams)
    {
        // Dispatch to supported tools
        switch (name)
        {
            case "getCurrentWeather":
                {
                    var result = GetCurrentWeather(requestParams.Arguments);
                    return new CallToolResult
                    {
                        Content = new List<ContentBlock> { new TextContentBlock { Text = result.Description } as ContentBlock },
                        StructuredContent = JsonSerializer.SerializeToNode(result, options),
                        IsError = false
                    };
                }
            case "GuessTheNumber":
                {
                    var result = await GuessTheNumber(requestParams.Arguments, default);
                    return new CallToolResult
                    {
                        Content = new List<ContentBlock> { new TextContentBlock { Text = result } },
                        IsError = false
                    };
                }
            case "RunAWhile":
                {
                    var result = await RunAWhile(requestParams.Arguments, default);
                    return new CallToolResult
                    {
                        Content = new List<ContentBlock> { new TextContentBlock { Text = result } },
                        IsError = false
                    };
                }
            default:
                return new CallToolResult
                {
                    IsError = true,
                    Content = new List<ContentBlock> { new TextContentBlock { Text = $"unknown tool: {name}" } }
                };
        }
    }

    private static async Task<string> RunAWhile(IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken token)
    {
        int delayInSeconds = 15;

        if (arguments != null)
        {
            if (arguments.TryGetValue("seconds", out var delayElem) && delayElem.ValueKind == JsonValueKind.Number)
            {
                delayInSeconds = delayElem.GetInt32();
            }
        }

        // Simulate a long-running task
        await Task.Delay(delayInSeconds * 1000, token);
        return $"RunAWhile completed after a delay of {delayInSeconds} seconds.";
    }

    private static GetCurrentWeatherResult GetCurrentWeather(IReadOnlyDictionary<string, JsonElement>? arguments)
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

        return new GetCurrentWeatherResult
        {
            Temperature = temp,
            Unit = unit,
            Description = $"Clear skies in {location}"
        };
    }

    private static async Task<string> GuessTheNumber(IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken token)
    {
        // First ask the user if they want to play
        var playSchema = new RequestSchema
        {
            Properties =
            {
                ["Answer"] = new BooleanSchema()
            }
        };

        var playResponse = await RequestsEndpoints.ElicitAsync(new ElicitRequestParams
        {
            Message = "Do you want to play a game?",
            RequestedSchema = playSchema
        }, token);

        // Check if user wants to play
        if (playResponse.Action != "accept" || playResponse.Content?["Answer"].ValueKind != JsonValueKind.True)
        {
            return "Maybe next time!";
        }

        // Now ask the user to enter their name
        var nameSchema = new RequestSchema
        {
            Properties =
            {
                ["Name"] = new StringSchema()
                {
                    Description = "Name of the player",
                    MinLength = 2,
                    MaxLength = 50,
                }
            }
        };

        var nameResponse = await RequestsEndpoints.ElicitAsync(new ElicitRequestParams
        {
            Message = "What is your name?",
            RequestedSchema = nameSchema
        }, token);

        if (nameResponse.Action != "accept")
        {
            return "Maybe next time!";
        }
        string? playerName = nameResponse.Content?["Name"].GetString();

        // Generate a random number between 1 and 10
        Random random = new Random();
        int targetNumber = random.Next(1, 11); // 1 to 10 inclusive
        int attempts = 0;

        var message = "Guess a number between 1 and 10";

        while (true)
        {
            attempts++;

            var guessSchema = new RequestSchema
            {
                Properties =
                {
                    ["Guess"] = new NumberSchema()
                    {
                        Type = "integer",
                        Minimum = 1,
                        Maximum = 10,
                    }
                }
            };

            var guessResponse = await RequestsEndpoints.ElicitAsync(new ElicitRequestParams
            {
                Message = message,
                RequestedSchema = guessSchema
            }, token);

            if (guessResponse.Action != "accept")
            {
                return "Maybe next time!";
            }
            int guess = (int)(guessResponse.Content?["Guess"].GetInt32())!;

            // Check if the guess is correct
            if (guess == targetNumber)
            {
                return $"Congratulations {playerName}! You guessed the number {targetNumber} in {attempts} attempts!";
            }
            else if (guess < targetNumber)
            {
                message = $"Your guess is too low! Try again (Attempt #{attempts}):";
            }
            else
            {
                message = $"Your guess is too high! Try again (Attempt #{attempts}):";
            }
        }
    }

    public IMockTools.ToolTypeEnum? ToolType(string name)
    {
        switch (name)
        {
            case "getCurrentWeather":
                return IMockTools.ToolTypeEnum.Standard;
            case "GuessTheNumber":
                return IMockTools.ToolTypeEnum.LongRunning;
            case "RunAWhile":
                return IMockTools.ToolTypeEnum.LongRunning;
            default:
                return null;
        }
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

public class GetCurrentWeatherResult
{
    /// <summary>The current temperature</summary>
    public double Temperature { get; set; }
    /// <summary>The unit of temperature, either 'celsius' or 'fahrenheit'</summary>
    public string Unit { get; set; } = string.Empty;
    /// <summary>A brief description of the current weather</summary>
    public string Description { get; set; } = string.Empty;
}

public class RunAWhileInput
{
    /// <summary>The number of seconds the tool should take to run</summary>
    public int seconds { get; set; } = 15;
}