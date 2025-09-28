namespace PureHttpTransport.Models;

using ModelContextProtocol.Protocol;

/// <summary>
/// An optional notification from the server to the client, informing it that the list of resources it can read from has changed. This may be issued by servers without any previous subscription from the client.
/// </summary>
public sealed class ResourceListChangedNotification : IServerNotification
{
    /// <summary>
    /// Always "notifications/resources/list_changed"
    /// </summary>
    public string Method { get; set; } = "notifications/resources/list_changed";

    /// <summary>
    /// Parameters for the resource list changed notification.
    /// </summary>
    public ResourceListChangedNotificationParams Params { get; set; } = new();
}