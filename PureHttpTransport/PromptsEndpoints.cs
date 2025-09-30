using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Protocol;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PureHttpTransport;

public static class PromptsEndpoints
{
    public static IMockPrompts? MockPrompts { get; set; } = null;

    public static IEndpointRouteBuilder MapPromptsEndpoints(this IEndpointRouteBuilder app)
    {
        var prompts = app.MapGroup("/prompts").WithTags("Prompts");
        prompts.AddEndpointFilter<ProtocolVersionFilter>();

        // List prompts
        prompts.MapGet("/", Ok<ListPromptsResult> (
            [Description("An opaque token representing the current pagination position. If provided, the server should return results starting after this cursor.")]
            string? cursor
        ) =>
        {
            var result = new ListPromptsResult
            {
                Prompts = MockPrompts?.ListPrompts()?.ToList() ?? new List<Prompt>()
            };
            return TypedResults.Ok<ListPromptsResult>(result);
        })
        .WithName("ListPrompts")
        .WithDescription("List available prompts");

        // Get a prompt by name and render with optional params in the request body
        prompts.MapGet("/{name}", Results<Ok<GetPromptResult>, NotFound<ProblemDetails>> (
            [Description("The name of the prompt or prompt template")]
            string name,
            [Description("The arguments for the GetPromptRequest.params")]
            [FromHeader(Name = "Mcp-Arguments")]
            string? argumentsHeader,
            [Description("The value of _meta for the request")]
            [FromHeader(Name = "Mcp-Meta")]
            string? metaHeader
        ) =>
        {
            // Reconstruct GetPromptRequest.params
            // If there is an Mcp-Arguments header, parse the value into a dictionary of string to JsonElement
            Dictionary<string, JsonElement> arguments = new();
            if (argumentsHeader is not null)
            {
                try
                {
                    arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsHeader)
                                   ?? new Dictionary<string, JsonElement>();
                }
                catch (JsonException)
                {
                    // Optionally log or handle invalid JSON
                }
            }
            // Now check for an Mcp-Meta header and if it exists, parse it into a dictionary of string to JsonElement
            // and add it to mcpArguments under the key "_meta"
            JsonObject? meta = new();
            if (metaHeader is not null)
            {
                try
                {
                    // convert meta to a JsonElement
                    meta = JsonSerializer.Deserialize<JsonObject>(metaHeader) ;
                }
                catch (JsonException)
                {
                    // Optionally log or handle invalid JSON
                }
            }
            var requestParams = new GetPromptRequestParams()
            {
                Name = name,
                Arguments = arguments,
                Meta = meta
            };
            var result = MockPrompts?.GetPrompt(requestParams);
            if (result == null)
            {
                return TypedResults.NotFound<ProblemDetails>(new()
                {
                    Detail = $"prompt with name {name} not found"
                });
            }

            return TypedResults.Ok<GetPromptResult>(result);
        })
        .WithName("GetPrompt")
        .WithDescription("Get and render a named prompt");

        return app;
    }
}

public interface IMockPrompts
{
    IEnumerable<Prompt> ListPrompts();
    GetPromptResult? GetPrompt(GetPromptRequestParams parameters);
}