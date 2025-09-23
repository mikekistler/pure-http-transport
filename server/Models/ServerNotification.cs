using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;

namespace PureHttpTransport.Models;

/// <summary>
/// Base type for all server notifications.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "Method")]
[JsonDerivedType(typeof(CancelledNotification), "notifications/cancelled")]
[JsonDerivedType(typeof(LoggingMessageNotification), "notifications/message")]
[JsonDerivedType(typeof(ProgressNotification), "notifications/progress")]
[JsonDerivedType(typeof(PromptListChangedNotification), "notifications/prompts/list_changed")]
[JsonDerivedType(typeof(ResourceListChangedNotification), "notifications/resources/list_changed")]
[JsonDerivedType(typeof(ResourceUpdatedNotification), "notifications/resources/updated")]
[JsonDerivedType(typeof(ToolListChangedNotification), "notifications/tools/list_changed")]
public abstract class ServerNotification { }
