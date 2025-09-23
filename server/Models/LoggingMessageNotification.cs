using ModelContextProtocol.Protocol;

namespace PureHttpTransport.Models;

/// <summary>
/// Notification of a log message passed from server to client. If no logging/setLevel request has been sent from the client, the server MAY decide which messages to send automatically.
/// </summary>
public sealed class LoggingMessageNotification : IServerNotification
{
    /// <summary>
    /// Always "notifications/message"
    /// </summary>
    public string Method { get; set; } = "notifications/message";

    /// <summary>
    /// Parameters for the logging message notification.
    /// </summary>
    public LoggingMessageNotificationParams Params { get; set; } = new();
}