using ModelContextProtocol.Protocol;

namespace PureHttpTransport.Models;

/// <summary>
/// This notification can be sent by either side to indicate that it is cancelling a previously-issued request.
/// The request SHOULD still be in-flight, but due to communication latency, it is always possible that this notification MAY arrive after the request has already finished.
/// This notification indicates that the result will be unused, so any associated processing SHOULD cease.
/// A client MUST NOT attempt to cancel its `initialize` request.
/// </summary>
public sealed class CancelledNotification : ServerNotification
{
    /// <summary>
    /// Always "notifications/cancelled"
    /// </summary>
    public string Method { get; set; } = "notifications/cancelled";

    /// <summary>
    /// Parameters for the cancellation notification.
    /// </summary>
    public CancelledNotificationParams Params { get; set; } = new();
}
