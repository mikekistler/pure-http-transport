using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;

namespace PureHttpTransport.Models;

/// <summary>
/// Base type for all server notifications.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "Method")]
[JsonDerivedType(typeof(CancelledNotification), "notifications/cancelled")]
[JsonDerivedType(typeof(InitializedNotification), "notifications/initialized")]
[JsonDerivedType(typeof(ProgressNotification), "notifications/progress")]
[JsonDerivedType(typeof(RootsListChangedNotification), "notifications/roots/list_changed")]
public interface IClientNotification { }
