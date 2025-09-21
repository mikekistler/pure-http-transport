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

        app.MapToolsEndpoints();

        return app;
    }
}