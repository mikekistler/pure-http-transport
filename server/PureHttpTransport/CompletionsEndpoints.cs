using ModelContextProtocol.Protocol;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using System.ComponentModel;

namespace PureHttpTransport;

public static class CompletionsEndpoints
{
    public static IEndpointRouteBuilder MapCompletionsEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /completions - client requests a completion from the server
        app.MapPost("/completions", Results<Ok<CompleteResult>, BadRequest<string>> (
            [Description("The completion request parameters")]
            [FromBody] CompleteRequestParams body) =>
        {
            if (body == null)
            {
                return TypedResults.BadRequest("Missing request body");
            }

            var result = new CompleteResult
            {
                Completion = new Completion
                {
                    Values = new List<string> { "Value1", "Value2", "Value3" },
                    Total = 3
                }
            };

            return TypedResults.Ok<CompleteResult>(result);
        })
        .WithName("CreateCompletion")
        .WithSummary("Create a completion (demo implementation)");

        return app;
    }
}
