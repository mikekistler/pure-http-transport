using System.ComponentModel.DataAnnotations;

namespace PureHttpTransport.Models;

/// <summary>
/// The server's response to a tools/list request from the client.
/// </summary>
public partial class ListToolsResult
{

    /// <summary>
    /// See [General fields: `_meta`](/specification/2025-06-18/basic/index#meta) for notes on `_meta` usage.
    /// </summary>
    public Dictionary<string, object> _meta { get; set; } = default!;

    /// <summary>
    /// An opaque token representing the pagination position after the last returned result.
    /// If present, there may be more results available.
    /// </summary>
    public string NextCursor { get; set; } = default!;

    [Required]
    public List<Tool> Tools { get; set; } = new List<Tool>();
}
