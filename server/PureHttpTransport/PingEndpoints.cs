using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PureHttpTransport;

public static class PingEndpoints
{
    public static IEndpointRouteBuilder MapPingEndpoint(this IEndpointRouteBuilder app)
    {
        var ping = app.MapGroup("/ping").WithTags("Ping");

        ping.MapGet("/", () => Results.StatusCode(StatusCodes.Status202Accepted))
            .WithName("Ping")
            .WithSummary("Ping the server; returns 202 Accepted to indicate reachability");

        return app;
    }
}
