using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OpenApi;

namespace PureHttpTransport.OpenApiExtensions;

public static class RequestIdTransformer
{
    // An extension method to add the Mcp-Request-Id header to OpenAPI documentation.
    public static OpenApiOptions AddRequestIdTransformer(this OpenApiOptions options)
    {
        // In requests, the Mcp-Request-Id header is documented as a header parameter.
        var parameter = new OpenApiParameter
        {
            Name = "Mcp-Request-Id",
            In = ParameterLocation.Header,
            Description = "The unique request ID for tracking purposes",
            Required = false,
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
        };
        // In responses, the Mcp-Request-Id header is documented as a response header.
        var header = new OpenApiHeader
        {
            Description = "The unique request ID for tracking purposes",
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
        };
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.Parameters ??= new Dictionary<string, OpenApiParameter>();
            document.Components.Parameters.Add("McpRequestId", parameter);
            document.Components.Headers ??= new Dictionary<string, OpenApiHeader>();
            document.Components.Headers.Add("McpRequestId", header);
            return Task.CompletedTask;
        });
        options.AddOperationTransformer((operation, context, cancellationToken) =>
        {
            // Add the Mcp-Request-Id header parameter to each operation.
            operation.Parameters ??= new List<OpenApiParameter>();
            operation.Parameters.Add(new OpenApiParameter
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.Parameter,
                    Id = "McpRequestId"
                }
            });

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