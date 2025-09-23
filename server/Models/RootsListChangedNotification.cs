using ModelContextProtocol.Protocol;

namespace PureHttpTransport.Models;

/// <summary>
/// A notification from the client to the server, informing it that the list of roots has changed.
/// This notification should be sent whenever the client adds, removes, or modifies any root.
/// The server should then request an updated list of roots using the ListRootsRequest.
/// </summary>
public class RootsListChangedNotification : IClientNotification
{
    /// <summary>
    /// The method name for this notification. Always "notifications/roots/list_changed".
    /// </summary>
    public string Method { get; set; } = "notifications/roots/list_changed";

    /// <summary>
    /// Optional parameters for the notification.
    /// </summary>
    public RootsListChangedNotificationParams Params { get; set; } = new();
}
