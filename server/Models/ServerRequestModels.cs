using System.Collections.Generic;

namespace PureHttpTransport.Models
{
    public class ServerRequestEnvelope
    {
        // MCP request id for cancellation mapping
        public string McpRequestId { get; set; } = System.Guid.NewGuid().ToString();

        // The request body, one of several types; keep generic for now
        public object Body { get; set; } = new Dictionary<string, object>();
    }

    public class CreateMessageRequestParams
    {
        public int MaxTokens { get; set; }
        public List<object> Messages { get; set; } = new List<object>();
    }

    public class ElicitRequestParams
    {
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> RequestedSchema { get; set; } = new Dictionary<string, object>();
    }

    public class ListRootsRequestParams
    {
        // No params for now
    }
}
