using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.ComponentModel;

using PureHttpMcpServer.Resources;
using ModelContextProtocol.Protocol;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;

namespace PureHttpTransport;

public static class ResourcesEndpoints
{
    public static IEndpointRouteBuilder MapResourcesEndpoints(this IEndpointRouteBuilder app)
    {
        var resources = app.MapGroup("/resources").WithTags("Resources");
        resources.AddEndpointFilter<ProtocolVersionFilter>();

        // List resources
        resources.MapGet("/", (
            [Description("An opaque token representing the current pagination position. If provided, the server should return results starting after this cursor.")]
            string? cursor
        ) =>
        {
            return TypedResults.Ok(MockResources.ListResources());
        })
        .WithName("ListResources")
        .WithDescription("List all available resources.");

        // List resource templates
        resources.MapGet("/templates", (
            [Description("An opaque token representing the current pagination position. If provided, the server should return results starting after this cursor.")]
            string? cursor
        ) =>
        {
            return TypedResults.Ok(MockResources.ListResourceTemplates());
        })
        .WithName("ListResourceTemplates")
        .WithDescription("List all available resource templates.");

        // Read a resource
        resources.MapPost("/", Results<Ok<ReadResourceResult>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>> (
            [Description("The parameters to get a resource provided by a server.")]
            ReadResourceRequestParams requestParams
        ) =>
        {
            if (requestParams == null || string.IsNullOrEmpty(requestParams.Uri))
            {
                return TypedResults.BadRequest<ProblemDetails>(new() { Detail = "Missing URI" });
            }

            var contents = MockResources.GetResourceContents(requestParams.Uri);

            if (contents == null)
            {
                return TypedResults.NotFound<ProblemDetails>(new() { Detail = "Resource not found" });
            }
            var result = new ReadResourceResult()
            {
                Contents = contents
            };
            return TypedResults.Ok<ReadResourceResult>(result);
        })
        .WithName("ReadResource")
        .WithDescription("Read a resource with a specific resource URI.");

        // Subscribe to a resource
        resources.MapPost("/subscribe", Results<Accepted, BadRequest<ProblemDetails>, NotFound<ProblemDetails>> (
            [Description("The parameters to subscribe to a resource provided by a server.")]
            SubscribeRequestParams requestParams
        ) =>
        {
            if (requestParams == null || string.IsNullOrEmpty(requestParams.Uri))
            {
                return TypedResults.BadRequest<ProblemDetails>(new () { Detail = "Missing URI" });
            }

            var resource = MockResources.GetResource(requestParams.Uri);
            if (resource == null)
            {
                return TypedResults.NotFound<ProblemDetails>(new () { Detail = "Resource not found" });
            }

            return TypedResults.Accepted("about:blank");
        })
        .WithName("SubscribeResource")
        .WithDescription("Subscribe to changes for a specific resource URI.");

        // Unsubscribe
        resources.MapPost("/unsubscribe", Results<Accepted, BadRequest<ProblemDetails>, NotFound<ProblemDetails>>(
            UnsubscribeRequestParams requestParams) =>
        {
            if (requestParams == null || string.IsNullOrEmpty(requestParams.Uri))
            {
                return TypedResults.BadRequest<ProblemDetails>(new () { Detail = "Missing URI" });
            }

            var resource = MockResources.GetResource(requestParams.Uri);
            if (resource == null)
            {
                return TypedResults.NotFound<ProblemDetails>(new () { Detail = "Resource not found" });
            }

            return TypedResults.Accepted("about:blank");
        })
        .WithName("UnsubscribeResource")
        .WithDescription("Unsubscribe from changes for a specific resource URI.");

        return app;
    }
}
