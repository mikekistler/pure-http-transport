using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace PureHttpTransport;

public class ProtocolVersionFilter : IEndpointFilter
{
    private const string HeaderName = "Mcp-Protocol-Version";
    private const string RequiredVersion = "2025-06-18";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        if (!httpContext.Request.Headers.TryGetValue(HeaderName, out var version) || version != RequiredVersion)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid or missing protocol version header.",
                Detail = $"The '{HeaderName}' header must be set to '{RequiredVersion}'."
            });
        }
        return await next(context);
    }
}
