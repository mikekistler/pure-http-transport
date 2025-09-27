using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PureHttpTransport.Models;
using System;
using Microsoft.AspNetCore.Http.HttpResults;

namespace PureHttpTransport;

public static class RequestsEndpoints
{
    private class RequestEntry(IServerRequest request)
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public IServerRequest request { get; init; } = request;
        public DateTime? PendingSince { get; set; } = null;
    }

    // Storage
    private static readonly ConcurrentQueue<RequestEntry> _requestQueue = new();
    private static readonly ConcurrentQueue<RequestEntry> _pendingQueue = new();

    public static void EnqueueRequest(IServerRequest request)
    {
        _requestQueue.Enqueue(new RequestEntry(request));
    }

    // Configurable timeout and timer for reactivation
    public static int PendingTimeoutMilliseconds = 30000; // default 30s
    private static readonly Timer _reactivationTimer;
    private static readonly TimeSpan _reactivationInterval = TimeSpan.FromMilliseconds(5000);

    static RequestsEndpoints()
    {
        // Start a timer to re-activate stale pending requests
        _reactivationTimer = new Timer(_ => ReactivatePending(), null, _reactivationInterval, _reactivationInterval);
    }

    public static IEndpointRouteBuilder MapRequestsEndpoints(this IEndpointRouteBuilder app)
    {
        var requests = app.MapGroup("/requests").WithTags("Requests");
        requests.AddEndpointFilter<ProtocolVersionFilter>();

        requests.MapGet("/", Results<Ok<IServerRequest>, NoContent> (HttpResponse response) =>
        {
            // Dequeue until we find an active request
            if (_requestQueue.TryDequeue(out var entry))
            {
                entry.PendingSince = DateTime.UtcNow;
                _pendingQueue.Enqueue(entry);

                // Set required headers
                response.Headers["Mcp-Request-Id"] = entry.Id;
                response.Headers["MCP-Protocol-Version"] = "2025-06-18";

                return TypedResults.Ok<IServerRequest>(entry.request);
            }

            return TypedResults.NoContent();
        })
        .WithName("GetServerRequest")
        .WithDescription("Get server-initiated requests (one at a time)");

        return app;
    }

    private static void ReactivatePending()
    {
        var now = DateTime.UtcNow;
        while (_pendingQueue.TryPeek(out var entry))
        {
            if (entry.PendingSince.HasValue && (now - entry.PendingSince.Value).TotalMilliseconds > PendingTimeoutMilliseconds)
            {
                // Timeout exceeded, re-activate this request
                if (_pendingQueue.TryDequeue(out var timedOutEntry))
                {
                    timedOutEntry.PendingSince = null;
                    _requestQueue.Enqueue(timedOutEntry);
                }
            }
            else
            {
                // The rest are still within the timeout
                break;
            }
        }
    }

    // Public helper to force reactivation in tests
    public static void ForceReactivatePending()
    {
        ReactivatePending();
    }
}
