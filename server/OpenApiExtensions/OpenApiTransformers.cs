using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OpenApi;

namespace PureHttpTransport.OpenApiExtensions;

public static class OpenApiTransformers
{
    // List of operation IDs that represent request operations needing the Mcp-Request-Id header.
    static string[] requestOperations = ["CreateCompletion", "Initialize", "SetLogLevel", "Ping", "GetPrompt", "ListPrompts", "ListResources", "GetResource", "ListTools", "CallTool"];
    static string[] listOperations = ["ListPrompts", "ListResources", "ListTools"];

    const string McpRequestIdParameterName = "McpRequestId";
    const string McpRequestIdHeaderName = "McpRequestId";
    const string ETagHeaderName = "ETag";

    const string OAuth2SecuritySchemeName = "OAuth2";

    // An extension method to add the Mcp-Request-Id header to OpenAPI documentation.
    public static OpenApiOptions AddOpenApiTransformers(this OpenApiOptions options)
    {
        // Add the Mcp-Request-Id header parameter, Mcp-Request-Id response header, and
        // ETag response header to the OpenAPI document components.
        options.AddDocumentTransformer(AddComponents);

        // Add the Mcp-Request-Id header parameter to each request operation
        options.AddOperationTransformer(AddRequestIdParameter);

        // Add the ETag response header to the 200 responses of ListPrompts, ListResources, and ListTools
        options.AddOperationTransformer(AddETagHeader);

        // Add the Mcp-Request-Id response header to the 200 response of GetServerRequest
        options.AddOperationTransformer(AddMcpRequestIdResponseHeader);

        // Add security scheme definitions to the OpenAPI document components.
        options.AddDocumentTransformer(AddSecuritySchemeDefinitions);

        // Add the OAuth2 security requirement to all operations
        options.AddOperationTransformer(AddSecurityRequirements);

        return options;
    }

    private static Task AddComponents(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var parameter = new OpenApiParameter
        {
            Name = PureHttpTransport.McpRequestIdHeader,
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

        // List operations may return an ETag header for caching purposes.

        var etagHeader = new OpenApiHeader
        {
            Description = "The ETag for the current version of the resource",
            Schema = new OpenApiSchema { Type = "string" }
        };

        document.Components ??= new OpenApiComponents();
        document.Components.Parameters ??= new Dictionary<string, OpenApiParameter>();
        document.Components.Parameters.Add(McpRequestIdParameterName, parameter);
        document.Components.Headers ??= new Dictionary<string, OpenApiHeader>();
        document.Components.Headers.Add(McpRequestIdHeaderName, requestIdHeader);
        document.Components.Headers.Add(ETagHeaderName, etagHeader);

        return Task.CompletedTask;
    }

    private static Task AddRequestIdParameter(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.Parameter,
                Id = McpRequestIdParameterName
            }
        });
        return Task.CompletedTask;
    }
    private static Task AddETagHeader(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (listOperations.Contains(operation.OperationId))
        {
            var okResponse = operation.Responses.GetValueOrDefault("200")!;
            okResponse.Headers ??= new Dictionary<string, OpenApiHeader>();
            okResponse.Headers.Add("ETag", new OpenApiHeader
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.Header,
                    Id = "ETag"
                }
            });
        }
        return Task.CompletedTask;
    }

    private static Task AddMcpRequestIdResponseHeader(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
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
    }

    private static Task AddSecuritySchemeDefinitions(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var identityUrl = "https://localhost:5001";
        var scopes = new Dictionary<string, string>
        {
            { "api1", "Access API 1" },
            { "api2", "Access API 2" }
        };
        var securityScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows()
            {
                // TODO: Change this to use Authorization Code flow with PKCE
                Implicit = new OpenApiOAuthFlow()
                {
                    AuthorizationUrl = new Uri($"{identityUrl}/connect/authorize"),
                    TokenUrl = new Uri($"{identityUrl}/connect/token"),
                    Scopes = scopes,
                }
            }
        };
        document.Components ??= new();
        document.Components.SecuritySchemes.Add(OAuth2SecuritySchemeName, securityScheme);
        return Task.CompletedTask;
    }

    private static Task AddSecurityRequirements(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        operation.Security ??= new List<OpenApiSecurityRequirement>();

        var securityRequirement = new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = OAuth2SecuritySchemeName
                    }
                },
                new List<string> { "api1", "api2" } // Scopes required for this operation
            }
        };

        operation.Security.Add(securityRequirement);

        return Task.CompletedTask;
    }
}
