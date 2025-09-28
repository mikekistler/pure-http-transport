using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PureHttpTransport.Models;
using System;
using Microsoft.AspNetCore.Http.HttpResults;
using ModelContextProtocol.Protocol;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace PureHttpTransport;

public static class RequestsEndpoints
{
    // Configurable timeout and timer for reactivation
    public static int PendingTimeoutMilliseconds = 30000; // default 30s
    private static readonly Timer _reactivationTimer;
    private static readonly TimeSpan _reactivationInterval = TimeSpan.FromMilliseconds(5000);

    private static readonly ILogger _logger;

    static RequestsEndpoints()
    {
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("RequestsEndpoints");

        // Start a timer to re-activate stale pending requests
        _reactivationTimer = new Timer(_ => ReactivatePending(), null, _reactivationInterval, _reactivationInterval);
    }

    private class RequestEntry(IServerRequest request)
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public IServerRequest request { get; init; } = request;
        public DateTime? PendingSince { get; set; } = null;
        public TaskCompletionSource<Result> tcs = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    // For async request/response correlation
    private static readonly ConcurrentDictionary<string, TaskCompletionSource<Result>> _pendingResponses = new();

    // Storage
    private static readonly ConcurrentQueue<RequestEntry> _requestQueue = new();
    private static readonly ConcurrentDictionary<string, RequestEntry> _pending = new();
    private static readonly ConcurrentQueue<string> _pendingQueue = new();

    public static async Task<Result> EnqueueRequestAsync(IServerRequest request)
    {
        var entry = new RequestEntry(request);
        _requestQueue.Enqueue(entry);
        // Await the response
        var response = await entry.tcs.Task;
        _pendingResponses.TryRemove(entry.Id, out _);
        return response;
    }

    public static async Task<ElicitResult> ElicitAsync(ElicitRequestParams requestParams, CancellationToken token = default)
    {
        var request = new ElicitRequest(requestParams);
        var result = await EnqueueRequestAsync(request);
        return result as ElicitResult ?? throw new InvalidOperationException("Expected ElicitResult");
    }

    // Call this when a response is received at /responses
    public static bool HandleResponse(string requestId, Result result)
    {
        if (_pending.TryGetValue(requestId, out var entry))
        {
            _pending.TryRemove(requestId, out _);
            // We should probably check the type of result matches the request type

            entry.tcs.SetResult(result);
            return true;
        }
        return false;
    }

    public static IEndpointRouteBuilder MapRequestsEndpoints(this IEndpointRouteBuilder app)
    {
        var requests = app.MapGroup("/requests").WithTags("Requests");
        requests.AddEndpointFilter<ProtocolVersionFilter>();

        requests.MapGet("/", Results<Ok<IServerRequest>, NoContent> (HttpResponse response) =>
        {
            // Dequeue until we find an active request
            while (_requestQueue.TryDequeue(out var entry))
            {
                if (!entry.tcs.Task.IsCompleted)
                {
                    entry.PendingSince = DateTime.UtcNow;
                    _pending[entry.Id] = entry;
                    _pendingQueue.Enqueue(entry.Id);

                    // Set required headers
                    response.Headers[PureHttpTransport.McpRequestIdHeader] = entry.Id;
                    response.Headers[PureHttpTransport.McpProtocolVersionHeader] = "2025-06-18";

                    return TypedResults.Ok<IServerRequest>(entry.request);
                }
            }

            return TypedResults.NoContent();
        })
        .WithName("GetServerRequest")
        .WithDescription("Get server-initiated requests (one at a time)");

        // Client responses to server requests
        requests.MapPost("/", Results<Accepted, BadRequest<ProblemDetails>> (
            [Description("The ID of the request being responded to.")]
            [FromHeader(Name = PureHttpTransport.McpRequestIdHeader)] string requestId,

            [Description("The result of the request.")]
            [FromBody] Result result
        ) =>
        {
            if (HandleResponse(requestId, result))
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

    private static void ReactivatePending()
    {
        var now = DateTime.UtcNow;

        // Find all the entries in _pending that are stale, and re-enqueue them

        var staleEntries = _pending.Where(kvp => kvp.Value.PendingSince.HasValue && (now - kvp.Value.PendingSince.Value).TotalMilliseconds > PendingTimeoutMilliseconds).ToList();
        foreach (var kvp in staleEntries)
        {
            _logger.LogWarning($"Reactivating stale pending request {kvp.Key} after {(now - kvp.Value.PendingSince!.Value).TotalMilliseconds}ms");
            kvp.Value.PendingSince = null;
            _requestQueue.Enqueue(kvp.Value);
        }
    }

    // Public helper to force reactivation in tests
    public static void ForceReactivatePending()
    {
        ReactivatePending();
    }
}
