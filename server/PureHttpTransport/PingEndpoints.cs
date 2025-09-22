using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PureHttpTransport;

public static class PingEndpoints
{
    public static IEndpointRouteBuilder MapPingEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/ping", () => Results.StatusCode(StatusCodes.Status202Accepted))
            .WithName("Ping")
            .WithSummary("Ping the server; returns 202 Accepted to indicate reachability");

        return app;
    }
}
