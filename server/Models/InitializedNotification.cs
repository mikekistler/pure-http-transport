
using ModelContextProtocol.Protocol;

namespace PureHttpTransport.Models;

/// <summary>
/// This notification is sent from the client to the server after initialization has finished.
/// </summary>
public class InitializedNotification : IClientNotification
{
	/// <summary>
	/// The method name for this notification. Always "notifications/initialized".
	/// </summary>
	public string Method { get; set; } = "notifications/initialized";

	/// <summary>
	/// Optional parameters for the notification.
	/// </summary>
	public InitializedNotificationParams Params { get; set; } = new();
}

