using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OpenApi;

namespace PureHttpTransport.OpenApiExtensions;

public static class OpenApiTransformers
{
    // List of operation IDs that represent request operations needing the Mcp-Request-Id header.
    static string[] requestOperations = ["CreateCompletion", "Initialize", "SetLogLevel", "Ping", "GetPrompt", "ListPrompts", "ListResources", "GetResource", "ListTools", "CallTool"];

    // An extension method to add the Mcp-Request-Id header to OpenAPI documentation.
    public static OpenApiOptions AddOpenApiTransformers(this OpenApiOptions options)
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
        var requestIdHeader = new OpenApiHeader
        {
            Description = "The unique request ID for tracking purposes",
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
        };
        // Add the parameter and header to the OpenAPI document components.
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.Parameters ??= new Dictionary<string, OpenApiParameter>();
            document.Components.Parameters.Add("McpRequestId", parameter);
            document.Components.Headers ??= new Dictionary<string, OpenApiHeader>();
            document.Components.Headers.Add("McpRequestId", requestIdHeader);
            return Task.CompletedTask;
        });
        // And an operation transformer to add the Mcp-Request-Id header parameter to each request operation
        options.AddOperationTransformer((operation, context, cancellationToken) =>
        {
            if (operation is not null && requestOperations.Contains(operation.OperationId))
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
            }
            return Task.CompletedTask;
        });
        // Add the Mcp-Request-Id response header to the 200 response of GetServerRequest
        options.AddOperationTransformer((operation, context, cancellationToken) =>
        {
            if (operation.OperationId == "GetServerRequest")
            {
                var okResponse = operation.Responses.GetValueOrDefault("200")!;
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