using Microsoft.AspNetCore.Builder;

namespace PureHttpTransport;

public static class PureHttpTransport
{
    public static IEndpointRouteBuilder UsePureHttpTransport(this IEndpointRouteBuilder app)
    {
        // Configure your API endpoints here
        // Example:
        // app.MapControllers();
        // app.MapGet("/api/endpoint", handler);

        app.MapCompletionsEndpoints();
        app.MapInitializeEndpoint();
        app.MapLogLevelEndpoints();
        app.MapNotificationsEndpoints();
        app.MapPingEndpoint();
        app.MapPromptsEndpoints();
        app.MapRequestsEndpoints();
        app.MapResourcesEndpoints();
        app.MapToolsEndpoints();

        return app;
    }
}