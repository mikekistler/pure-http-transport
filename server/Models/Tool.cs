using System.ComponentModel.DataAnnotations;

namespace PureHttpTransport.Models;

/// <summary>
/// Definition for a tool the client can call.
/// </summary>
public partial class Tool
{
    /// <summary>
    /// See [General fields: `_meta`](/specification/2025-06-18/basic/index#meta) for notes on `_meta` usage.
    /// </summary>
    public Dictionary<string, object> _meta { get; set; } = default!;

    /// <summary>
    /// Optional additional tool information.
    /// Display name precedence order is: title, annotations.title, then name.
    /// </summary>
    public ToolAnnotations Annotations { get; set; } = default!;

    /// <summary>
    /// A human-readable description of the tool.
    /// This can be used by clients to improve the LLM's understanding of available tools. It can be thought of like a "hint" to the model.
    /// </summary>
    public string Description { get; set; } = default!;

    /// <summary>
    /// A JSON Schema object defining the expected parameters for the tool.
    /// </summary>
    [Required]
    public Schema InputSchema { get; set; } = new Schema();

    /// <summary>
    /// Intended for programmatic or logical use, but used as a display name in past specs or fallback (if title isn't present).
    /// </summary>
    [Required(AllowEmptyStrings = true)]
    public string Name { get; set; } = default!;

    /// <summary>
    /// An optional JSON Schema object defining the structure of the tool's output returned in
    /// the structuredContent field of a CallToolResult.
    /// </summary>
    public Schema OutputSchema { get; set; } = default!;

    /// <summary>
    /// Intended for UI and end-user contexts — optimized to be human-readable and easily understood,
    /// even by those unfamiliar with domain-specific terminology.
    ///
    /// If not provided, the name should be used for display (except for Tool,
    /// where `annotations.title` should be given precedence over using `name`,
    /// if present).
    /// </summary>
    public string Title { get; set; } = default!;
}
