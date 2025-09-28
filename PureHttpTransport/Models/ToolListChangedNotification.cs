using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;

namespace PureHttpTransport.Models;

/// <summary>
/// An optional notification from the server to the client, informing it that the list of tools it offers has changed. This may be issued by servers without any previous subscription from the client.
/// </summary>
public sealed class ToolListChangedNotification : IServerNotification
{
    /// <summary>
    /// Parameters for the tool list changed notification.
    /// </summary>
    [JsonPropertyName("params")]
    public ToolListChangedNotificationParams Params { get; set; } = new();
}