using ModelContextProtocol.Protocol;

namespace PureHttpTransport.Models;

/// <summary>
/// An optional notification from the server to the client, informing it that the list of prompts it offers has changed. This may be issued by servers without any previous subscription from the client.
/// </summary>
public sealed class PromptListChangedNotification : ServerNotification
{
    /// <summary>
    /// Always "notifications/prompts/list_changed"
    /// </summary>
    public string Method { get; set; } = "notifications/prompts/list_changed";

    /// <summary>
    /// Parameters for the prompt list changed notification.
    /// </summary>
    public PromptListChangedNotificationParams Params { get; set; } = new();
}