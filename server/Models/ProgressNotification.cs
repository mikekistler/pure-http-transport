using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;

namespace PureHttpTransport.Models;

/// <summary>
/// An out-of-band notification used to inform the receiver of a progress update for a long-running request.
/// </summary>
public sealed class ProgressNotification : ServerNotification
{
    /// <summary>
    /// Always "notifications/progress"
    /// </summary>
    public string Method { get; set; } = "notifications/progress";

    /// <summary>
    /// Parameters for the progress notification.
    /// </summary>
    public ProgressNotificationParams Params { get; set; } = new();
}

public class ProgressNotificationParams
{
    /// <summary>
    /// Gets or sets metadata reserved by MCP for protocol-level metadata.
    /// </summary>
    [JsonPropertyName("_meta")]
    public JsonObject? Meta { get; set; }

    /// <summary>
    /// Gets or sets the progress token which was given in the initial request, used to associate this notification with
    /// the corresponding request.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This token acts as a correlation identifier that links progress updates to their corresponding request.
    /// </para>
    /// <para>
    /// When an endpoint initiates a request with a <see cref="ProgressToken"/> in its metadata,
    /// the receiver can send progress notifications using this same token. This allows both sides to
    /// correlate the notifications with the original request.
    /// </para>
    /// </remarks>
    public ProgressToken ProgressToken { get; init; }

    /// <summary>
    /// Gets or sets the progress thus far.
    /// </summary>
    /// <remarks>
    /// This should increase for each notification issued as part of the same request, even if the total is unknown.
    /// </remarks>
    public ProgressNotificationValue Progress { get; init; }
}

/// <summary>
/// A progress value that can be sent using ProgressNotificationValue.
/// </summary>
public struct ProgressNotificationValue
{
    /// <summary>
    /// Gets or sets the progress thus far.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This value typically represents either a percentage (0-100) or the number of items processed so far (when used with the <see cref="Total"/> property).
    /// </para>
    /// <para>
    /// When reporting progress, this value should increase monotonically as the operation proceeds.
    /// Values are typically between 0 and 100 when representing percentages, or can be any positive number
    /// when representing completed items in combination with the <see cref="Total"/> property.
    /// </para>
    /// </remarks>
    public float? Progress { get; init; }

    /// <summary>Gets or sets the total number of items to process (or total progress required), if known.</summary>
    public float? Total { get; init; }

    /// <summary>Gets or sets an optional message describing the current progress.</summary>
    public string? Message { get; init; }
}
