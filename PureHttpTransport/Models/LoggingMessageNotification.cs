using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;

namespace PureHttpTransport.Models;

/// <summary>
/// Notification of a log message passed from server to client. If no logging/setLevel request has been sent from the client, the server MAY decide which messages to send automatically.
/// </summary>
public sealed class LoggingMessageNotification : IServerNotification
{
    /// <summary>
    /// Parameters for the logging message notification.
    /// </summary>
    [JsonPropertyName("params")]
    public LoggingMessageNotificationParams Params { get; set; } = new();
}