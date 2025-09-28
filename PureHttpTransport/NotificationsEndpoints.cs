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
    private class NotificationGroup(List<IServerNotification> items)
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public List<IServerNotification> Items { get; init; } = items;
        public DateTimeOffset? PendingSince { get; init; } = System.DateTimeOffset.UtcNow;
    }

    // These fields manage the state of notification groups:
    // - _activeQueue: queue of notifications ready to be delivered to clients
    // - _pending: groups that have been delivered but not yet acknowledged
    private static readonly ConcurrentQueue<IServerNotification> _activeQueue = new();
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
            // If there are any notifications ready to send, dequeue and return them
            List<IServerNotification> notifications = new();
            while (_activeQueue.TryDequeue(out var notification))
            {
                notifications.Add(notification);
            }

            // Return an empty list if no notifications
            if (notifications.Count == 0)
            {
                return TypedResults.Ok(Array.Empty<IServerNotification>());
            }

            // Create a notification group for this batch if we have any and add it to pending

            var group = new NotificationGroup(notifications);
            _pending[group.Id] = group;

            // Send the notifications with the group ID in the header
            response.Headers[PureHttpTransport.McpGroupIdHeader] = group.Id;
            return TypedResults.Ok(notifications.ToArray());
        })
        .WithName("GetNotifications")
        .WithDescription("Get server notifications (groups)");

        // POST /notifications to send notifications from client to server and acknowledge a group

        notifications.MapPost("/", Accepted (
            [Description("A collection of notifications being sent from client to server.")]
            IClientNotification[] notifications,

            [FromHeader(Name = PureHttpTransport.McpGroupIdHeader)] string? groupId) =>
        {
            // First check for groupId to acknowledge previously sent notifications from server to client
            if (!string.IsNullOrEmpty(groupId))
            {
                _pending.TryRemove(groupId, out var _);
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
            foreach (var item in items)
            {
                _activeQueue.Enqueue(item);
            }
        })
        .WithName("EnqueueNotifications")
        .ExcludeFromDescription();

        return app;
    }

    public static void EnqueueNotification(IServerNotification notification)
    {
        _activeQueue.Enqueue(notification);
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
                    foreach (var item in removed.Items)
                    {
                        _activeQueue.Enqueue(item);
                    }
                }
            }
        }
    }

    // Test helper
    public static void ForceReactivatePendingNotifications() => ReactivatePending();
}
