using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Protocol;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

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
            HttpRequest request // for reading parameters passed in headers
        ) =>
        {
            var prompt = MockPrompts?.GetPrompt(name);
            if (prompt == null)
            {
                return TypedResults.NotFound<ProblemDetails>(new()
                {
                    Detail = $"prompt with name {name} not found"
                });
            }

            var renderedContent = prompt.Description;
            var result = new GetPromptResult
            {
                Description = renderedContent,
                Messages = new List<PromptMessage>
                {
                    new PromptMessage
                    {
                        Role = Role.Assistant,
                        Content = new TextContentBlock
                        {
                            Text = renderedContent ?? string.Empty
                        }
                    }
                }
            };
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
    Prompt? GetPrompt(string name);
}