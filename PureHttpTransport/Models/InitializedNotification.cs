
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;

namespace PureHttpTransport.Models;

/// <summary>
/// This notification is sent from the client to the server after initialization has finished.
/// </summary>
public class InitializedNotification : IClientNotification
{
	/// <summary>
	/// Optional parameters for the notification.
	/// </summary>
	[JsonPropertyName("params")]
	public InitializedNotificationParams Params { get; set; } = new();
}

