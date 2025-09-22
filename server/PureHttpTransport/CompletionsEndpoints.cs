using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PureHttpTransport;

public static class CompletionsEndpoints
{
    public static IEndpointRouteBuilder MapCompletionsEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /completions - client requests a completion from the server
        app.MapPost("/completions", async (HttpRequest req, HttpResponse res, object? body) =>
        {
            if (body == null)
            {
                res.StatusCode = StatusCodes.Status400BadRequest;
                await res.WriteAsync("Missing request body");
                return Results.StatusCode(StatusCodes.Status400BadRequest);
            }

            // Very small demo completion result
            var result = new
            {
                id = Guid.NewGuid().ToString(),
                model = "demo-model",
                choices = new[] {
                    new { text = "Hello world", index = 0 }
                },
                usage = new { prompt_tokens = 0, completion_tokens = 2, total_tokens = 2 }
            };

            return Results.Ok(result);
        })
        .WithName("CreateCompletion")
        .WithSummary("Create a completion (demo implementation)");

        return app;
    }
}
