using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PureHttpTransport.Models;
using System;

namespace PureHttpTransport;

public static class RequestsEndpoints
{
    private class RequestEntry
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public object Body { get; init; } = new Dictionary<string, object>();
        public DateTime? PendingSince { get; set; }
        public string State { get; set; } = "active"; // active, pending, completed
    }

    // Storage
    private static readonly ConcurrentDictionary<string, RequestEntry> _store = new();
    private static readonly ConcurrentQueue<string> _activeQueue = new();
    private static readonly ConcurrentDictionary<string, RequestEntry> _pending = new();

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

        requests.MapGet("/", (HttpResponse response) =>
        {
            // Dequeue until we find an active request
            while (_activeQueue.TryDequeue(out var id))
            {
                if (_store.TryGetValue(id, out var entry) && entry.State == "active")
                {
                    // Mark pending
                    entry.State = "pending";
                    entry.PendingSince = DateTime.UtcNow;
                    _pending[id] = entry;

                    // Set required headers
                    response.Headers["Mcp-Request-Id"] = entry.Id;
                    response.Headers["MCP-Protocol-Version"] = "2025-06-18";

                    return Results.Json(entry.Body);
                }
            }

            return Results.NoContent();
        })
        .WithName("GetServerRequest")
        .WithSummary("Get server-initiated requests (one at a time)");

        // Helper to enqueue server requests for tests or internal use
        app.MapPost("/internal/enqueueRequest", (object body) =>
        {
            var entry = new RequestEntry { Body = body };
            _store[entry.Id] = entry;
            _activeQueue.Enqueue(entry.Id);
            return Results.Ok(new { id = entry.Id });
        })
        .WithName("EnqueueRequest")
        .ExcludeFromDescription();

        return app;
    }

    private static void ReactivatePending()
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _pending.ToArray())
        {
            var id = kv.Key;
            var entry = kv.Value;
            if (entry.PendingSince.HasValue && (now - entry.PendingSince.Value).TotalMilliseconds > PendingTimeoutMilliseconds)
            {
                // Move back to active
                if (_pending.TryRemove(id, out var removed))
                {
                    removed.State = "active";
                    removed.PendingSince = null;
                    _store[id] = removed;
                    _activeQueue.Enqueue(id);
                }
            }
        }
    }

    // Public helper to force reactivation in tests
    public static void ForceReactivatePending()
    {
        ReactivatePending();
    }
}
