using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Protocol;

namespace PureHttpTransport;

public static class LogLevelEndpoints
{
    public static LoggingLevel CurrentLogLevel { get; private set; } = LoggingLevel.Info;

    public static IEndpointRouteBuilder MapLogLevelEndpoints(this IEndpointRouteBuilder app)
    {
        var logLevel = app.MapGroup("/logLevel").WithTags("Logging");

        logLevel.MapPost("/", Results<NoContent, BadRequest<ProblemDetails>> (SetLevelRequestParams body) =>
        {
            if (body == null)
            {
                return TypedResults.BadRequest<ProblemDetails>(new()
                {
                    Detail = "Missing body"
                });
            }

            CurrentLogLevel = body.Level;

            return TypedResults.NoContent();
        })
        .WithName("SetLogLevel");

        // Test helper to read current level
        app.MapGet("/internal/getLogLevel", () => Results.Ok(new { level = CurrentLogLevel }))
            .WithName("GetLogLevel")
            .ExcludeFromDescription();

        return app;
    }
}
