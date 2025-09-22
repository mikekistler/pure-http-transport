using System.Collections.Generic;

namespace PureHttpTransport.Models
{
    /// <summary>
    /// The server's response to a tool call.
    /// </summary>
    public class CallToolResult
    {
        public Dictionary<string, object> _meta { get; set; } = new Dictionary<string, object>();

        // A list of content blocks representing unstructured output. Kept as object
        // so callers can return simple typed content (e.g., anonymous objects)
        public List<object> content { get; set; } = new List<object>();

        public bool? isError { get; set; }

        // An optional structured JSON result
        public Dictionary<string, object>? structuredContent { get; set; }
    }
}
