using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;
using Microsoft.AspNetCore.Http.HttpResults;
using ModelContextProtocol.Protocol;

using PureHttpTransport.Models;
using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;

namespace PureHttpTransport;

public static class NotificationsEndpoints
{
    private class NotificationGroup
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public List<IServerNotification> Items { get; init; } = new List<IServerNotification>();
        public DateTime? PendingSince { get; set; }
        public string State { get; set; } = "active"; // active, pending, completed
    }

    // These fields manage the state of notification groups:
    // - _groups: all notification groups by ID (active, pending, or completed)
    // - _activeQueue: queue of group IDs ready to be delivered to clients
    // - _pending: groups that have been delivered but not yet acknowledged
    private static readonly ConcurrentDictionary<string, NotificationGroup> _groups = new();
    private static readonly ConcurrentQueue<string> _activeQueue = new();
    private static readonly ConcurrentDictionary<string, NotificationGroup> _pending = new();

    // PendingTimeoutMilliseconds: how long (ms) a group can remain pending before being reactivated
    public static int PendingTimeoutMilliseconds = 30000;

    // Timer and interval for periodically reactivating timed-out pending groups
    private static readonly Timer _reactivationTimer;
    private static readonly TimeSpan _reactivationInterval = TimeSpan.FromMilliseconds(5000);

    static NotificationsEndpoints()
    {
        _reactivationTimer = new Timer(_ => ReactivatePending(), null, _reactivationInterval, _reactivationInterval);
    }

    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        var notifications = app.MapGroup("/notifications").WithTags("Notifications");
        notifications.AddEndpointFilter<ProtocolVersionFilter>();

        // GET /notifications returns an array of notifications (may be empty)
        notifications.MapGet("/", Ok<IServerNotification[]> (HttpResponse response) =>
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

                    return TypedResults.Ok(group.Items.ToArray());
                }
            }

            // No group available: return empty array
            return TypedResults.Ok(Array.Empty<IServerNotification>());
        })
        .WithName("GetNotifications")
        .WithDescription("Get server notifications (groups)");

        // POST /notifications to send notifications from client to server and acknowledge a group
        notifications.MapPost("/", Accepted (
            [Description("A collection of notifications being sent from client to server.")]
            IClientNotification[] notifications,

            [FromHeader(Name = "Mcp-Group-Id")] string? groupId ) =>
        {
            // First check for groupId to acknowledge previously sent notifications from server to client
            if (!string.IsNullOrEmpty(groupId))
            {
                if (_pending.TryRemove(groupId, out var group))
                {
                    group.State = "completed";
                    _groups.TryRemove(groupId, out _);
                }
            }

            // Process incoming notifications from client to server
            foreach (var notification in notifications)
            {
                switch (notification)
                {
                    case CancelledNotification cancelled:
                        // Handle cancellation
                        Console.WriteLine($"Received cancellation for request: {cancelled.Params.RequestId}");
                        break;
                    case InitializedNotification initialized:
                        // Handle initialization
                        Console.WriteLine("Client initialized.");
                        break;
                    case RootsListChangedNotification rootsChanged:
                        // Handle roots list change
                        Console.WriteLine("Roots list changed.");
                        break;
                    default:
                        var method = (notification as dynamic).Method;
                        Console.WriteLine($"Received unknown notification type: {method}");
                        break;
                }
            };

            return TypedResults.Accepted("about:blank");
        })
        .WithName("AcknowledgeNotifications")
        .WithDescription("Acknowledge a previously received group of notifications");

        // Internal helper to enqueue a group of notifications (for tests)
        app.MapPost("/internal/enqueueNotifications", (List<IServerNotification> items) =>
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
