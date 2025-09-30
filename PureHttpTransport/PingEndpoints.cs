namespace PureHttpTransport;

public static class PingEndpoints
{
    public static IEndpointRouteBuilder MapPingEndpoint(this IEndpointRouteBuilder app)
    {
        var ping = app.MapGroup("/ping").WithTags("Ping");
        ping.AddEndpointFilter<ProtocolVersionFilter>();

        ping.MapGet("/", () => TypedResults.Accepted("about:blank"))
            .WithName("Ping")
            .WithDescription("Ping the server; returns 202 Accepted to indicate reachability");

        return app;
    }
}
