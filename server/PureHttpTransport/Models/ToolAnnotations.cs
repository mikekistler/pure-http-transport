
namespace PureHttpTransport.Models;

/// <summary>
/// Additional properties describing a Tool to clients.
///
/// NOTE: all properties in ToolAnnotations are **hints**.
/// They are not guaranteed to provide a faithful description of
/// tool behavior (including descriptive properties like `title`).
///
/// Clients should never make tool use decisions based on ToolAnnotations
/// received from untrusted servers.
/// </summary>

public partial class ToolAnnotations
{
    /// <summary>
    /// If true, the tool may perform destructive updates to its environment.
    /// If false, the tool performs only additive updates.
    ///
    /// (This property is meaningful only when `readOnlyHint == false`)
    ///
    /// Default: true
    /// </summary>
    public bool DestructiveHint { get; set; } = default!;

    /// <summary>
    /// If true, calling the tool repeatedly with the same arguments
    /// will have no additional effect on the its environment.
    ///
    /// (This property is meaningful only when `readOnlyHint == false`)
    ///
    /// Default: false
    /// </summary>
    public bool IdempotentHint { get; set; } = default!;

    /// <summary>
    /// If true, this tool may interact with an "open world" of external
    /// entities. If false, the tool's domain of interaction is closed.
    /// For example, the world of a web search tool is open, whereas that
    /// of a memory tool is not.
    ///
    /// Default: true
    /// </summary>
    public bool OpenWorldHint { get; set; } = default!;

    /// <summary>
    /// If true, the tool does not modify its environment.
    ///
    /// Default: false
    /// </summary>
    public bool ReadOnlyHint { get; set; } = default!;

    /// <summary>
    /// A human-readable title for the tool.
    /// </summary>
    public string Title { get; set; } = default!;
}
