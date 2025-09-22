using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OpenApi;

namespace PureHttpTransport.OpenApiExtensions;

public static class RequestIdTransformer
{
    // An extension method to add the Mcp-Request-Id header to OpenAPI documentation.
    public static OpenApiOptions AddRequestIdTransformer(this OpenApiOptions options)
    {
        // In responses, the Mcp-Request-Id header is documented as a response header.
        var requestIdHeader = new OpenApiHeader
        {
            Description = "The unique request ID for tracking purposes",
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
        };
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Components.Headers ??= new Dictionary<string, OpenApiHeader>();
            document.Components.Headers.Add("McpRequestId", requestIdHeader);
            return Task.CompletedTask;
        });
        options.AddOperationTransformer((operation, context, cancellationToken) =>
        {
            // Add the Mcp-Request-Id response header to the 200 response if it exists.
            if (operation.Responses.TryGetValue("200", out var okResponse))
            {
                okResponse.Headers ??= new Dictionary<string, OpenApiHeader>();
                okResponse.Headers.Add("McpRequestId", new OpenApiHeader
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.Header,
                        Id = "McpRequestId"
                    }
                });
            }

            return Task.CompletedTask;
        });
        return options;
    }
}