using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PureHttpTransport.Models;
using System;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using ModelContextProtocol.Protocol;

namespace PureHttpTransport;

public static class ResponsesEndpoints
{
    public static IEndpointRouteBuilder MapResponsesEndpoints(this IEndpointRouteBuilder app)
    {
        var responses = app.MapGroup("/responses").WithTags("Responses");
        responses.AddEndpointFilter<ProtocolVersionFilter>();

        // Client responses to server requests
        responses.MapPost("/", Results<Accepted, BadRequest<ProblemDetails>> (
            [Description("The ID of the request being responded to.")]
            [FromHeader(Name = PureHttpTransport.McpRequestIdHeader)] string requestId,

            [Description("The result of the request.")]
            [FromBody] Result result
        ) =>
        {
            if (RequestsEndpoints.HandleResponse(requestId, result))
            {
                return TypedResults.Accepted("about:blank");
            }

            return TypedResults.BadRequest<ProblemDetails>(new()
            {
                Detail = $"No pending request with ID {requestId} was found.",
            });
        })
        .WithName("ResponsesEndpoint")
        .WithDescription("Receive client responses to server requests");

        return app;
    }
}
