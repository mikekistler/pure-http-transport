using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;

namespace PureHttpTransport;

public static class NotificationsEndpoints
{
    private class NotificationGroup
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public List<object> Items { get; init; } = new List<object>();
        public DateTime? PendingSince { get; set; }
        public string State { get; set; } = "active"; // active, pending, completed
    }

    private static readonly ConcurrentDictionary<string, NotificationGroup> _groups = new();
    private static readonly ConcurrentQueue<string> _activeQueue = new();
    private static readonly ConcurrentDictionary<string, NotificationGroup> _pending = new();

    public static int PendingTimeoutMilliseconds = 30000;
    private static readonly Timer _reactivationTimer;
    private static readonly TimeSpan _reactivationInterval = TimeSpan.FromMilliseconds(5000);

    static NotificationsEndpoints()
    {
        _reactivationTimer = new Timer(_ => ReactivatePending(), null, _reactivationInterval, _reactivationInterval);
    }

    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var notifications = app.MapGroup("/notifications").WithTags("Notifications");

        // GET /notifications returns an array of notifications (may be empty)
        notifications.MapGet("/", (HttpResponse response) =>
        {
            // Dequeue a group if available
            while (_activeQueue.TryDequeue(out var id))
            {
                if (_groups.TryGetValue(id, out var group) && group.State == "active")
                {
                    // Mark pending
                    group.State = "pending";
                    group.PendingSince = DateTime.UtcNow;
                    _pending[id] = group;

                    response.Headers["Mcp-Notifications-Group-Id"] = group.Id;
                    response.Headers["MCP-Protocol-Version"] = "2025-06-18";

                    return Results.Json(group.Items);
                }
            }

            // No group available: return empty array
            response.Headers["MCP-Protocol-Version"] = "2025-06-18";
            return Results.Json(new object[0]);
        })
        .WithName("GetNotifications")
        .WithSummary("Get server notifications (groups)");

        // POST /notifications to acknowledge a group
        notifications.MapPost("/", async (HttpRequest req, HttpResponse res) =>
        {
            if (!req.Headers.TryGetValue("Mcp-Notifications-Group-Id", out var idValues))
            {
                res.StatusCode = StatusCodes.Status400BadRequest;
                await res.WriteAsync("Missing Mcp-Notifications-Group-Id header");
                return Results.StatusCode(StatusCodes.Status400BadRequest);
            }

            var id = idValues.First();
            if (string.IsNullOrEmpty(id))
            {
                res.StatusCode = StatusCodes.Status400BadRequest;
                await res.WriteAsync("Empty Mcp-Notifications-Group-Id header");
                return Results.StatusCode(StatusCodes.Status400BadRequest);
            }

            if (_pending.TryRemove(id, out var group))
            {
                group.State = "completed";
                _groups.TryRemove(id, out _);
                res.StatusCode = StatusCodes.Status202Accepted;
                await res.WriteAsync(string.Empty);
                return Results.StatusCode(StatusCodes.Status202Accepted);
            }
            else
            {
                res.StatusCode = StatusCodes.Status404NotFound;
                await res.WriteAsync("Notification group not found or not pending");
                return Results.StatusCode(StatusCodes.Status404NotFound);
            }
        })
        .WithName("AcknowledgeNotifications")
        .WithSummary("Acknowledge a previously received group of notifications");

        // Internal helper to enqueue a group of notifications (for tests)
        app.MapPost("/internal/enqueueNotifications", (List<object> items) =>
        {
            var group = new NotificationGroup { Items = items };
            _groups[group.Id] = group;
            _activeQueue.Enqueue(group.Id);
            return Results.Ok(new { id = group.Id });
        })
        .WithName("EnqueueNotifications")
        .ExcludeFromDescription();

        return app;
    }

    private static void ReactivatePending()
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _pending.ToArray())
        {
            var id = kv.Key;
            var group = kv.Value;
            if (group.PendingSince.HasValue && (now - group.PendingSince.Value).TotalMilliseconds > PendingTimeoutMilliseconds)
            {
                if (_pending.TryRemove(id, out var removed))
                {
                    removed.State = "active";
                    removed.PendingSince = null;
                    _groups[id] = removed;
                    _activeQueue.Enqueue(id);
                }
            }
        }
    }

    // Test helper
    public static void ForceReactivatePendingNotifications() => ReactivatePending();
}
