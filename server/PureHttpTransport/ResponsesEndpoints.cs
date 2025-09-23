using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PureHttpTransport.Models;
using System;

namespace PureHttpTransport;

public static class ResponsesEndpoints
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

    static ResponsesEndpoints()
    {
        // Start a timer to re-activate stale pending requests
        _reactivationTimer = new Timer(_ => ReactivatePending(), null, _reactivationInterval, _reactivationInterval);
    }

    public static IEndpointRouteBuilder MapResponsesEndpoints(this IEndpointRouteBuilder app)
    {
        var responses = app.MapGroup("/responses").WithTags("Responses");
        responses.AddEndpointFilter<ProtocolVersionFilter>();

        // Client responses to server requests
        responses.MapPost("/", async (HttpRequest req, HttpResponse res) =>
        {
            // The client MUST include Mcp-Request-Id to identify which request this response completes
            if (!req.Headers.TryGetValue("Mcp-Request-Id", out var idValues))
            {
                res.StatusCode = StatusCodes.Status400BadRequest;
                await res.WriteAsync("Missing Mcp-Request-Id header");
                return Results.StatusCode(StatusCodes.Status400BadRequest);
            }

            var id = idValues.First();

            if (string.IsNullOrEmpty(id))
            {
                res.StatusCode = StatusCodes.Status400BadRequest;
                await res.WriteAsync("Empty Mcp-Request-Id header");
                return Results.StatusCode(StatusCodes.Status400BadRequest);
            }

            if (_pending.TryRemove(id, out var entry))
            {
                entry.State = "completed";
                _store.TryRemove(id, out _);
                res.StatusCode = StatusCodes.Status202Accepted;
                await res.WriteAsync(string.Empty);
                return Results.StatusCode(StatusCodes.Status202Accepted);
            }
            else
            {
                // Not found or already completed
                res.StatusCode = StatusCodes.Status404NotFound;
                await res.WriteAsync("Request not found or not pending");
                return Results.StatusCode(StatusCodes.Status404NotFound);
            }
        })
        .WithName("ResponsesEndpoint")
        .WithSummary("Receive client responses to server requests");

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
