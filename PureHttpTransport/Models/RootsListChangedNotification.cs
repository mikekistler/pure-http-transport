using System.Text.Json.Serialization;
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
    /// Optional parameters for the notification.
    /// </summary>
    [JsonPropertyName("params")]
    public RootsListChangedNotificationParams Params { get; set; } = new();
}
