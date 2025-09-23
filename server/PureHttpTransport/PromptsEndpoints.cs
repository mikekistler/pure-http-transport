using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PureHttpTransport;

public static class PromptsEndpoints
{
    private static readonly ConcurrentDictionary<string, string> _prompts = new()
    {
        ["greeting"] = "Hello, {{name}}!",
        ["confirm"] = "Please confirm: {{question}}",
    };

    public static IEndpointRouteBuilder MapPromptsEndpoints(this IEndpointRouteBuilder app)
    {
        var prompts = app.MapGroup("/prompts").WithTags("Prompts");
        prompts.AddEndpointFilter<ProtocolVersionFilter>();
        // List prompts
        prompts.MapGet("/", () => Results.Json(_prompts.Keys.ToArray()))
            .WithName("ListPrompts")
            .WithSummary("List available prompts");

        // Get a prompt by name and render with optional params in the request body
        prompts.MapPost("/{name}", async (string name, HttpRequest req) =>
        {
            if (!_prompts.TryGetValue(name, out var template))
            {
                return Results.NotFound(new { error = "prompt not found" });
            }

            Dictionary<string, object>? parameters = null;
            try
            {
                parameters = await req.ReadFromJsonAsync<Dictionary<string, object>>();
            }
            catch
            {
                // ignore parse errors and treat as no params
                parameters = null;
            }

            var rendered = template;
            if (parameters != null)
            {
                foreach (var kv in parameters)
                {
                    var token = "{{" + kv.Key + "}}";
                    var replacement = kv.Value?.ToString() ?? string.Empty;
                    rendered = rendered.Replace(token, replacement);
                }
            }

            return Results.Ok(new { text = rendered });
        })
        .WithName("GetPrompt")
        .WithSummary("Get and render a named prompt");

        // Internal helper to set a prompt (for tests)
        app.MapPost("/internal/setPrompt/{name}", (string name, object body) =>
        {
            var content = body?.ToString() ?? string.Empty;
            _prompts[name] = content;
            return Results.Ok(new { name = name, content = content });
        })
        .WithName("SetPrompt")
        .ExcludeFromDescription();

        return app;
    }
}
