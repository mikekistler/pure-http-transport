using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;

namespace PureHttpTransport.Models;

/// <summary>
/// A notification from the server to the client, informing it that a resource has changed and may need to be read again. This should only be sent if the client previously sent a resources/subscribe request.
/// </summary>
public sealed class ResourceUpdatedNotification : IServerNotification
{
    /// <summary>
    /// Parameters for the resource updated notification.
    /// </summary>
    [JsonPropertyName("params")]
    public ResourceUpdatedNotificationParams Params { get; set; } = new();
}