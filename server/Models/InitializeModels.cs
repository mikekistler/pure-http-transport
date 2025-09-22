using System.Collections.Generic;

namespace PureHttpTransport.Models
{
    public class InitializeRequestParams
    {
        public Dictionary<string, object>? Capabilities { get; set; }

        public Implementation? ClientInfo { get; set; }

        public string ProtocolVersion { get; set; } = "2025-06-18";
    }

    public class InitializeResult
    {
        public Dictionary<string, object> _meta { get; set; } = new Dictionary<string, object>();

        public Dictionary<string, object> Capabilities { get; set; } = new Dictionary<string, object>();

        public string ProtocolVersion { get; set; } = "2025-06-18";

        public Implementation ServerInfo { get; set; } = new Implementation();

        public string? Instructions { get; set; }
    }
}
