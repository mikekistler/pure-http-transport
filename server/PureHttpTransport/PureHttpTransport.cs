using Microsoft.AspNetCore.Builder;

namespace PureHttpTransport;

public static class PureHttpTransport
{
    public const string McpProtocolVersionHeader = "MCP-Protocol-Version";
    public const string McpRequestIdHeader = "Mcp-Request-Id";
    public const string McpGroupIdHeader = "Mcp-Group-Id";

    public static IEndpointRouteBuilder UsePureHttpTransport(this IEndpointRouteBuilder app)
    {
        app.MapCompletionsEndpoints();
        app.MapInitializeEndpoint();
        app.MapLogLevelEndpoints();
        app.MapNotificationsEndpoints();
        app.MapPingEndpoint();
        app.MapPromptsEndpoints();
        app.MapRequestsEndpoints();
        app.MapResourcesEndpoints();
        app.MapResponsesEndpoints();
        app.MapToolsEndpoints();

        return app;
    }
}