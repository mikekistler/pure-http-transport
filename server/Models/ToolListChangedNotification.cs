using ModelContextProtocol.Protocol;

namespace PureHttpTransport.Models;

/// <summary>
/// An optional notification from the server to the client, informing it that the list of tools it offers has changed. This may be issued by servers without any previous subscription from the client.
/// </summary>
public sealed class ToolListChangedNotification : ServerNotification
{
    /// <summary>
    /// Always "notifications/tools/list_changed"
    /// </summary>
    public string Method { get; set; } = "notifications/tools/list_changed";

    /// <summary>
    /// Parameters for the tool list changed notification.
    /// </summary>
    public ToolListChangedNotificationParams Params { get; set; } = new();
}