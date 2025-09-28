namespace PureHttpTransport.Models;

using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;

/// <summary>
/// An optional notification from the server to the client, informing it that the list of resources it can read from has changed. This may be issued by servers without any previous subscription from the client.
/// </summary>
public sealed class ResourceListChangedNotification : IServerNotification
{
    /// <summary>
    /// Parameters for the resource list changed notification.
    /// </summary>
    [JsonPropertyName("params")]
    public ResourceListChangedNotificationParams Params { get; set; } = new();
}