using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PureHttpTransport;

public static class LogLevelEndpoints
{
    private static readonly HashSet<string> _allowed = new() { "trace", "debug", "info", "warn", "error", "fatal", "off" };

    public static string CurrentLogLevel { get; private set; } = "info";

    public class LogLevelParams
    {
        public string? level { get; set; }
    }

    public static IEndpointRouteBuilder MapLogLevelEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/logLevel", async (LogLevelParams? body, HttpResponse res) =>
        {
            if (body == null || string.IsNullOrEmpty(body.level))
            {
                res.StatusCode = StatusCodes.Status400BadRequest;
                await res.WriteAsync("Missing or empty level");
                return Results.StatusCode(StatusCodes.Status400BadRequest);
            }

            var lvl = body.level!.ToLowerInvariant();
            if (!_allowed.Contains(lvl))
            {
                res.StatusCode = StatusCodes.Status400BadRequest;
                await res.WriteAsync("Invalid level");
                return Results.StatusCode(StatusCodes.Status400BadRequest);
            }

            CurrentLogLevel = lvl;
            return Results.Ok(new { level = CurrentLogLevel });
        })
        .WithName("SetLogLevel");

        // Test helper to read current level
        app.MapGet("/internal/getLogLevel", () => Results.Ok(new { level = CurrentLogLevel }))
            .WithName("GetLogLevel");

        return app;
    }
}
