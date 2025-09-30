# SEP-xxxx: Pure HTTP Transport for Model Context Protocol (MCP)

<!-- markdownlint-disable MD024 -->

**Status:** draft
**Type:** Standards Track
**Created:** 2025-09-08
**Authors:** Mike Kistler

## Abstract

This SEP proposes a Pure HTTP transport layer for the Model Context Protocol (MCP). The proposed transport will enable
scalable and efficient communication between MCP clients and servers using the mature and proven HTTP protocol, without the complexities of JSON-RPC over HTTP.

## Motivation

The HTTP protocol is widely adopted and supported across various platforms and programming languages, making it an ideal choice for a transport layer in the MCP ecosystem. HTTP was designed to be scalable, reliable, and efficient, which aligns well with the needs of enterprise MCP servers.

Unlike local MCP Servers, remote MCP Servers are multi-tenant (accessed by multiple clients simultaneously). Enterprise MCP Servers must be scalable and fault tolerant and this is accomplished by creating a cluster of nodes running the MCP Server code with a load balancer that distributes incoming requests across the cluster. The Pure HTTP transport is designed to facilitate this architecture.

## Specification

The complete technical specification for this SEP will be provided in a forthcoming PR. Here we provide an overview of the key design elements and decisions.

The Pure HTTP transport for MCP will utilize standard HTTP methods (GET, POST, DELETE) to perform operations defined in the MCP protocol. Each MCP operation will be mapped to a specific HTTP endpoint, allowing clients to interact with the MCP server using standard HTTP requests.

The transport will also define a set of HTTP headers to convey metadata and control information necessary for MCP operations, such as authentication tokens, request identifiers, and content types.

### Schema changes

The Pure HTTP transport will only flow the "payload" portion of the MCP messages over HTTP, without the JSON-RPC envelope.
This means that the request and response bodies will directly contain the parameters and results of MCP operations,
rather than being wrapped in a JSON-RPC structure. Some metadata from the JSON-RPC envelope may be conveyed using HTTP headers
when needed.

It would be helpful, though not strictly necessary, to modify the MCP schemas to separate the "payload" portion of each message
from the JSON-RPC envelope. These schema changes have already been proposed in [SEP-1319].

[SEP-1319]: https://github.com/modelcontextprotocol/modelcontextprotocol/issues/1319

### Error Responses

The Pure HTTP transport will use standard HTTP status codes for error conditions that occur during MCP operations.

| MCP Error Condition               | HTTP Status Code |
|-----------------------------------|------------------|
| Invalid Request                    | 400 Bad Request   |
| Unauthorized Access                | 401 Unauthorized  |
| Resource Not Found                 | 404 Not Found     |
| Method Not Allowed                 | 405 Method Not Allowed |
| Internal Server Error              | 500 Internal Server Error |

The response body for error conditions should contain the `error` field of the `JSONRPCError` schema,
which includes the `code` and `message` properties as defined in the [JSON-RPC error codes] specification.

[JSON-RPC error codes]: https://json-rpc.dev/docs/reference/error-codes

#### Example Error Response

```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "code": -32602,
  "message": "Unknown tool: invalid_tool_name"
}
```

### HTTP Methods

MCP list operations (e.g., `tools/list`), get/read operations (e.g., `resources/read`), and the `ping` operation
will use the HTTP GET method. Parameters for these operations will be passed either in headers or as query parameters in the URL

MCP allows any request to contain a "_meta" property with arbitrary metadata for the request. For operations mapped to HTTP GET, "_meta" will be passed in the "Mcp-Meta" header. The value of this header will be a JSON-encoded string representing the "_meta" object.

All other operations will use the HTTP POST method and pass parameters in the request body as JSON.

### HTTP Paths

The HTTP paths for MCP operations will follow a consistent pattern based on the operation name. Each operation will be mapped to a specific endpoint, with the operation name used as the path.

For example, the `tools/list` operation will be mapped to the `/tools` endpoint, while the `tools/call` operation will be mapped to the `/tools/{toolName}/calls` endpoint.

The forthcoming PR will provide a complete mapping of MCP operations to HTTP paths.

### HTTP Headers

The Pure HTTP transport will use HTTP headers to convey certain protocol metadata, including:

| Header Name               | Description                                      |
|---------------------------|--------------------------------------------------|
| Mcp-Protocol-Version      | Indicates the version of the MCP protocol being used in the request.                        |
| Mcp-Request-Id            | A globally unique identifier for MCP request messages. |

### Support for conditional requests

All list operations (e.g., `tools/list`, `resources/list`, `prompts/list`) will return an `etag` header in the response. The MCP Client can later poll the MCP Server by issuing another GET request with `if-none-match: etag`; this returns 304-NotModified without the list if the list hasn’t changed, or returns 200-OK with the new list if the list has changed.

Read operations that return a single resource (e.g., `resources/get`) will also return an `etag` header in the response. The MCP Client can later poll the MCP Server by issuing another GET request with `if-none-match: etag`; this returns 304-NotModified without the resource if it hasn’t changed, or returns 200-OK with the new resource if it has changed.

### Initialization

The Pure HTTP transport will support an initialization step that allows the MCP Client to exchange metadata (e.g. capabilities, instructions) with the MCP Server. The "initialize" MCP operation is mapped to an HTTP POST request to the "/initialize" endpoint. The request body will contain a JSON object representing the `InitializeRequest` schema, and the response body will contain a JSON object representing the `InitializeResult` schema.

### Sessions

The initial version of the Pure HTTP transport will not offer support for (transport-level) sessions. There is
considerable ambiguity and disagreement about the current session management feature of the
Streamable HTTP transport, and until there is consensus on the meaning / purpose / behavior
of sessions it is best to omit them from the Pure HTTP transport.

In the absence of sessions, servers will use the authentication context to determine what server
resources are appropriate to expose to the client. This is consistent with current RESTful services
like databases – a request can access any data that the user is authorized to access.

When sessions are better defined, it should be possible to add them to the Pure HTTP transport in a backward-compatible way.
The session support currently defined in the Streamable HTTP transport could easily be adapted to the Pure HTTP transport
by returning a "Mcp-Session-Id" header in the response to the "initialize" request, and accepting a "Mcp-Session-Id" header
in subsequent requests.

### Tools List example

A "tools/list" MCP request will be implemented as an HTTP GET request to the "/tools" endpoint. The `cursor` property of `ListToolsRequest.Params` will be passed as a query parameter named `cursor`. The response body will contain a JSON object representing the `ListToolsResult`.

```http
GET /tools?cursor=abc123 HTTP/1.1
Host: mcp.example.com
Accept: application/json
Mcp-Protocol-Version: 2025-06-18
Mcp-Request-ID: 0605a86c-b88b-4e8c-ada4-2433eccb3d73
Mcp-Meta: {"foobar":"bazqux"}
```

The response body will contain a JSON object representing the `ListToolsResult`.

```http
HTTP/1.1 200 OK
Content-Type: application/json
Etag: "def456"

{
  "_meta": {
    "requestID": "xyz789",
    "timestamp": "2025-09-08T12:34:56Z"
  },
  "tools": [
    {
      "name": "get_weather",
      "title": "Weather Information Provider",
      "description": "Get current weather information for a location",
      "inputSchema": { ... },
    }
  ],
  "nextCursor": "def456"
}
```

### Tool Call example

A "tools/call" MCP request will be implemented as an HTTP POST request to the "/tools/{toolName}/calls" endpoint. The body of the HTTP request will contain the JSON object representing the `params` field of the `ToolCallRequest`. The "name" field **SHOULD** be omitted from the body, since it is already specified in the URL path. If it is passed it will be ignored. The response body will contain a JSON object representing the `ToolCallResult`.

```http
POST /tools/get_weather/calls HTTP/1.1
Host: mcp.example.com
Content-Type: application/json
Accept: application/json
Mcp-Protocol-Version: 2025-06-18
Mcp-Request-ID: 0605a86c-b88b-4e8c-ada4-2433eccb3d73

{
  "arguments": {
    "location": "Seattle, WA"
  }
}
```

The response body will contain a JSON object representing the `ToolCallResult`.

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "_meta": {
    "requestID": "xyz789",
    "timestamp": "2025-09-08T12:34:56Z"
  },
  "content": [
    {
      "type": "text",
      "text": "Current weather in New York:\nTemperature: 72°F\nConditions: Partly cloudy"
    }
  ]
}
```

## Rationale

This section provides the rationale for design choices in the Pure HTTP Transport for MCP SEP that might be questioned. While the Pure HTTP transport specification follows the Streamable HTTP transport patterns where possible, there are intentional deviations to provide the scalability and reliability benefits of pure HTTP.

Decision: Use pure HTTP rather than JSON-RPC over HTTP

Rationale: Using pure HTTP simplifies the transport layer and avoids the complexities introduced by JSON-RPC. This decision aligns with the goal of maintaining a lightweight and efficient communication protocol.

## Backward Compatibility

Because this is an additional transport layer, there are no backward compatibility concerns. Existing stdio and Streamable HTTP transports remain unchanged and fully functional.

## Reference Implementation

An initial reference implementation has been developed in C#. It is currently in a private repository and will be made publicly available once the SEP is finalized.

## Future Considerations

### Compatibility with Future MCP Versions

This transport specification is designed to stay at the transport layer and should be compatible with future MCP protocol versions.

### Security Implications

This SEP has no additional security implications.

## References
