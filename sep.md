# SEP-1612: Fully Compliant and Backward-Compatible Pure HTTP Transport

<!-- markdownlint-disable MD024 -->

<!-- cspell:ignore resumability streamable -->

**Status:** draft
**Type:** Standards Track
**Created:** 2025-xx-xx
**Author:** Mike Kistler

## Abstract

This SEP proposes a Pure HTTP transport layer for the Model Context Protocol (MCP). This transport layer is designed to be fully compliant and backward-compatible with the 2025-06-18 MCP protocol while avoiding the complexities and overhead associated with JSON-RPC over HTTP.

Each JSON RPC message type is mapped to a specific HTTP method and path, with parameters passed in the request body or as query parameters. The transport layer also defines a set of HTTP headers to convey metadata and control information necessary for MCP operations.

## Motivation

The HTTP protocol is widely adopted and supported across various platforms and programming languages, making it an ideal choice for a transport layer in the MCP ecosystem. HTTP was designed to be scalable, reliable, and efficient, which aligns well with the needs of enterprise MCP servers.

Unlike local MCP Servers, remote MCP Servers are multi-tenant (accessed by multiple clients simultaneously). Enterprise MCP Servers must be scalable and fault tolerant and this is accomplished by creating a cluster of nodes running the MCP Server code with a load balancer that distributes incoming requests across the cluster. The Pure HTTP transport is designed to facilitate this architecture.

## Specification

The complete technical specification for this SEP will be provided in a forthcoming PR. Here we provide an overview of the key design elements and decisions.

The Pure HTTP transport for MCP will utilize standard HTTP methods (GET, POST, DELETE) to perform operations defined in the MCP protocol. Each MCP operation will be mapped to a specific HTTP endpoint, allowing clients to interact with the MCP server using standard HTTP requests.

The transport will also define a set of HTTP headers to convey metadata and control information necessary for MCP operations, such as authentication tokens, request identifiers, and content types.

### Schema changes

The Pure HTTP transport will only flow the "payload" portion of the MCP messages over HTTP, without the JSON-RPC envelope. This means that the request and response bodies will directly contain the parameters and results of MCP operations, rather than being wrapped in a JSON-RPC structure. Some metadata from the JSON-RPC envelope may be conveyed using HTTP headers when needed.

It would be helpful, though not strictly necessary, to modify the MCP schemas to separate the "payload" portion of each message from the JSON-RPC envelope. These schema changes have already been proposed in [SEP-1319].

[SEP-1319]: https://github.com/modelcontextprotocol/modelcontextprotocol/issues/1319

### Authorization

According to the [MCP specification](https://modelcontextprotocol.io/specification/2025-06-18/basic/authorization#protocol-requirements), authorization is strictly optional but when implemented it must adhere to the OAuth 2.1 and related standards.

As OAuth 2.1 is designed to work over HTTP, it is a natural fit for the Pure HTTP transport. The transport will support the use of OAuth 2.1 access tokens passed in the "Authorization" HTTP header using the "Bearer" scheme.

Servers **MUST** follow the [Security Best Practices](https://modelcontextprotocol.io/specification/2025-06-18/basic/security_best_practices) outlined in the MCP specification when implementing authorization.

### HTTP Scheme

The Pure HTTP transport will support both HTTP and HTTPS schemes. However, for security reasons, all MCP communications **SHOULD** use HTTPS to ensure data confidentiality and integrity.

### HTTP Methods

MCP list operations (e.g., `tools/list`), get/read operations (e.g., `resources/read`), and the `ping` operation will use the HTTP GET method. Parameters for these operations will be passed either in headers or as query parameters in the URL.

MCP allows any request to contain a "_meta" property with arbitrary metadata for the request. For operations mapped to HTTP GET, "_meta" will be passed in the "Mcp-Meta" header. The value of this header will be a JSON-encoded string representing the "_meta" object.

The following table summarizes the endpoints provided by the MCP Server using the Pure HTTP transport.

<!-- jq -r '.. | objects | if .properties.method.const then .properties.method.const else empty end' schema/2025-06-18/schema.json | sort -->

| HTTP Path            | Method | related MCP message   | Request Body                | Response                | Notes |
| -------------------- | ------ | --------------------- | --------------------------- | ----------------------- | ----- |
| /completions         | POST   | completion/complete   | CompleteRequest.params      | 200: CompleteResult     | (1)   |
| /initialize          | POST   | initialize            | InitializeRequest.params    | 200: InitializeResult   | (1)   |
| /logLevel            | POST   | logging/setLevel      | SetLevelRequest.params      | 202: Accepted           | (1)   |
| /notifications       | GET    | notifications/* (to client) |                       | 200: [ServerNotification] |     |
| /notifications       | POST   | notifications/* (to server) | [ClientNotification]  | 202: Accepted           |       |
| /ping                | GET    | ping (from client)    |                             | 202: Accepted           | (2)   |
| /prompts             | GET    | prompts/list          |                             | 200: ListPromptsResult  | (3,6) |
| /prompts/{name}      | GET    | prompts/get           |                             | 200: GetPromptResult    | (4)   |
| /requests            | POST   | _multiple_            |                             | 200: ServerRequest      | (7)   |
| /responses           | POST   | _multiple_            | ClientResult                | 202: Accepted           | (8)   |
| /resources           | GET    | resources/list        |                             | 200: ListResourcesResult | (3,6)|
| /resources/{uri}     | GET    | resources/read        |                             | 200: ReadResourceResult  | (5)  |
| /resources/subscribe | POST   | resources/subscribe   | SubscribeRequest.params     | 202: Accepted           | (1)   |
| /resources/templates | GET    | resources/templates/list |                          | 200: ListResourceTemplatesResult | (3) |
| /resources/unsubscribe | POST | resources/unsubscribe | UnsubscribeRequest.params   | 202: Accepted           | (1)   |
| /tools               | GET    | tools/list            |                             | 200: ListToolsResult    | (3,6) |
| /tools/{name}/calls  | POST   | tools/call            | CallToolRequest.params      | 200: CallToolResult     | (1)   |

Notes:
1. The request body includes the "_meta" property of "JSONRPCRequest".
2. _meta passed in "Mcp-Meta" header
3. cursor passed in query string; _meta and other parameters passed in headers
4. GetPromptRequest.params, including _meta, sent in headers.
5. URI of ReadResourceRequest.params sent in the path -- must be URL encoded. Remaining parameters, including _meta, sent in headers.
6. Response **SHOULD** include ETag to support caching of content
7. The response includes an "Mcp-Request-Id" header with a globally unique identifier for the request.
8. The request includes an "Mcp-Request-Id" header with the same value as the corresponding server-to-client request.

### HTTP Paths

The HTTP paths for MCP message will follow a consistent pattern based on the message "method". Each message method will be mapped to a specific endpoint.

For example, the `tools/list` operation will be mapped to the `/tools` endpoint, while the `tools/call` operation will be mapped to the `/tools/{toolName}/calls` endpoint.

The forthcoming PR will provide a complete mapping of MCP operations to HTTP paths.

### HTTP Headers

The Pure HTTP transport will use HTTP headers to convey certain protocol metadata, including:

| Header Name               | Description                                      |
|---------------------------|--------------------------------------------------|
| Mcp-Protocol-Version      | Indicates the version of the MCP protocol being used in the request.    |
| Mcp-Request-Id            | A globally unique identifier for MCP request messages. |
| Mcp-Group-Id              | A globally unique identifier for a group of MCP notifications sent from server to client, to support acknowledgement. |
| Mcp-Meta                  | Sends the contents of the _meta field, as serialized JSON, for requests sent with HTTP GET methods. |
| Mcp-Arguments             | Sends the contents of the arguments field, as serialized JSON, for requests sent with HTTP GET methods. |

### Error Responses

The Pure HTTP transport will use standard HTTP status codes for error conditions that occur during MCP operations.

| MCP Error Condition               | HTTP Status Code |
|-----------------------------------|------------------|
| Invalid Request                    | 400 Bad Request   |
| Unauthorized Access                | 401 Unauthorized  |
| Resource Not Found                 | 404 Not Found     |
| Method Not Allowed                 | 405 Method Not Allowed |
| Internal Server Error              | 500 Internal Server Error |

The response body for error conditions **MUST** contain the `error` field of the `JSONRPCError` schema, which **MUST** include the `code` and `message` properties as defined in the [JSON-RPC error codes] specification.

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

### Sending Messages from Server to Client

The server sends requests to the client, one at a time, in the response to an HTTP POST to "/requests".
The server sends notifications to the client, in batches, in the response to an HTTP GET to "/notifications".

1. The server **MUST** implement a POST method on the "/requests" endpoint and a GET on the "/notifications" endpoint to
send requests and notifications to the client.
2. The request body of the POST on "/requests" **MUST** be empty. The server **MUST** fail a POST to "/requests" with a 400 Bad Request error if the body is not empty.
2. The response of the POST on "/requests" **MUST** be a 200 with a "ServerRequest" body or 204: NoContent.
A 200 response also must include an Mcp-Request-Id response header with a globally unique identifier for the request.
3. The response body of the GET on "/notifications" **MUST** be an array of "ServerNotification" messages (which may be empty).
4. The response to a POST on "/requests" or GET on "/notifications":
  - **MUST** have "Content-Type" of `application/json`
  - **MUST** include the `MCP-Protocol-Version: <protocol-version>` header to specify the protocol version of the message and any response to the message.

#### Sending Requests from Sever to Client

When the server wishes to send a request to the client, it should add it to a collection of server-to-client requests and mark it "active". When the client issues a POST request to "/requests", the server should respond with the "oldest" "active" messages and then mark this message as "pending". This will prevent the same message from being immediately redelivered to the client on a subsequent POST to "/requests". When the server receives a response from the client to a pending request, it should be marked "complete" and its resources released. This design allows the client to receive and process multiple outstanding server-to-client requests concurrently.

Periodically the server should mark requests that have been "pending" for longer than some timeout period as "active" so that the request is redelivered to the client. Clients should implement logic to avoid duplicate processing of a retried request.

Responses to a server-to-client request **MUST** be sent in the body of a POST to the "/responses" endpoint. The response body **MUST** contain a JSON object that conforms to the appropriate variant of the `ClientResult` schema. The POST to "/responses" **MUST** include an "Mcp-Request-Id" header with the same value as the "Mcp-Request-Id" header in the corresponding server-to-client request. It must also include an "Mcp-Protocol-Version" header with the protocol version of the response.

Servers **SHOULD** implement a configurable limit on the number of "active" and "pending" requests that can be queued for delivery to the client. If this limit is reached, the server **MUST** reject new requests from being added to the queue with an appropriate error message. In addition, servers **MAY** implement a configurable policy for discarding old "active" requests to make room for new requests.

#### Sending Notifications from Server to Client

Notifications are sent from the server to the client in response to a GET request to the "/notifications" endpoint. Notifications are batched together and sent in the response body as an array of "ServerNotification" messages. If there are no notifications to send, the server **MUST** respond with an empty array.

The Pure HTTP Transport requires acknowledgement of notifications:

1. Each response to a GET request on "/notifications" **MUST** include an "Mcp-Message-Group" header with a globally unique value.
This header **MAY** be omitted if the GET returns an empty array.
2. When receiving the response of a GET request on "/notifications" that contains an "Mcp-Message-Group" header, the client **MUST** send a POST to the server's "/notifications" endpoint with an "Mcp-Message-Group" request header with the same value when all the notifications in the group have been delivered to the MCP Host.
  - When the server receives this POST, it **SHOULD** release any resources associated with the acknowledged notifications.
  - A POST to "/notifications" with a "Mcp-Message-Group" request header may also contain notifications to be delivered to
    the server in the request body.

The GET request to "/notifications" continues to return the same group of notifications until they are acknowledged by the client. Notifications that the server queues for delivery after sending a group of notifications to the client will be held until the client acknowledges the previous group.

### Initialization

The Pure HTTP transport will support an initialization step that allows the MCP Client to exchange metadata (e.g. capabilities, instructions) with the MCP Server. The "initialize" MCP operation is mapped to an HTTP POST request to the "/initialize" endpoint. The request body **MUST** contain a JSON object representing the `InitializeRequest` schema, and the response body **MUST** contain a JSON object representing the `InitializeResult` schema.

### Sessions

Following the principles of RESTful design, the Pure HTTP transport is stateless at the transport layer.

The initial version of the Pure HTTP transport will not offer support for (transport-level) sessions. There is considerable ambiguity and disagreement about the current session management feature of the Streamable HTTP transport, and until there is consensus on the meaning / purpose / behavior of sessions it is best to omit them from the Pure HTTP transport.

In the absence of sessions, servers will use the authentication context to determine what server resources are appropriate to expose to the client. This is consistent with current RESTful services like databases – a request can access any data that the user is authorized to access.

When sessions are better defined, it should be possible to add them to the Pure HTTP transport in a backward-compatible way. The session support currently defined in the Streamable HTTP transport could easily be adapted to the Pure HTTP transport by returning a "Mcp-Session-Id" header in the response to the "initialize" request, and accepting a "Mcp-Session-Id" header in subsequent requests.

### Resumability

In HTTP, there is no expectation of a persistent network connection between client and server, and the Pure HTTP transport embraces this principle. It therefore separates the concept of a logical MCP connection from the physical network connection.

A logical connection is associated with an authentication context, e.g., the "sub" (subject) claim of the OAuth token, or a sessionId when implemented. Clients do not need to maintain a persistent physical connection to the server, and can "resume" operations on the logical connection by simply sending requests with the same authentication context.

As described above in [Sending Messages from Server to Client](#sending-messages-from-server-to-client), the server queues requests and notifications for delivery on a logical connection and delivers them when the client issues a GET request to the appropriate endpoint. This allows clients to disconnect and later reconnect without losing messages.

Further, the server maintains state for requests and notifications sent to the client until they are acknowledged by the client. This ensures delivery of messages that may have been lost due to network issues or client disconnections.

### Support for conditional requests

The Conditional Requests feature of HTTP/1.1 (RFC 9110) can be used to implement efficient polling of lists and resources in MCP. This is an alternative to the "listChanged" and "resources/updated" notifications that servers, particularly when operating in stateless mode, may not wish to implement.

All list operations (e.g., `tools/list`, `resources/list`, `prompts/list`) should return an `etag` header in the response. The MCP Client can later poll the MCP Server by issuing another GET request with `if-none-match: etag`; this returns 304-NotModified without the list if the list hasn’t changed, or returns 200-OK with the new list if the list has changed.

Read operations that return a single resource (e.g., `resources/get`) should also return an `etag` header in the response. The MCP Client can later poll the MCP Server by issuing another GET request with `if-none-match: etag`; this returns 304-NotModified without the resource if it hasn’t changed, or returns 200-OK with the new resource if it has changed.

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

A prototype implementation has been developed for both the client and server in C# and is available at

https://github.com/mikekistler/pure-http-transport

## Planned extensions

A number of proposed improvements to MCP should be straightforward to implement in the Pure HTTP transport. These include:

### Streaming Tool Call results

Streaming does not fit the standard JSON RPC abstraction of a single request followed by a single response, but a common extension to JSON-RPC is to allow the response to be a stream of messages where a `final` property indicates the last message in the stream. For example, [the A2A protocol has adopted this extension](https://a2a-protocol.org/latest/specification/#93-streaming-task-execution-sse). If MCP chose to adopt this extension, it could be implemented in the Pure HTTP transport by allowing a tool call response to use the text/event-stream content type to send a stream of CallToolResult messages.

### Long-Running Tool Calls

The Pure HTTP transport can support long-running tool calls by allowing the server to return a 202 Accepted response with a Location header pointing to a status endpoint. The client can then poll this endpoint to check the status of the tool call until it is complete. The status endpoint returns 202 Accepted while the tool call is still in progress, and then returns a 200 OK response with the final result of the tool call once it is complete. The server retains the result of the tool call for a configurable period to allow the client to retrieve it.

The status endpoint could be implemented as "/tools/{toolName}/calls/{requestId}", where "requestId" is a unique identifier for the tool call.

### Webhooks

The Pure HTTP transport could support webhooks by allowing clients to register a callback URL with the server. The server would then send notifications to this URL instead of requiring the client to poll for notifications. This would reduce latency and improve efficiency for receiving notifications.

### Fault Tolerance

The Pure HTTP transport can be made more fault-tolerant by implementing retry logic for transient errors, such as network timeouts or server errors. Clients can implement exponential backoff strategies when retrying requests to avoid overwhelming the server.

The globally unique request IDs in the "Mcp-Request-Id" header can be used to detect and handle duplicate requests, ensuring idempotency for operations that may be retried.

### Load Balancing / Horizontal Scalability

For servers that need to scale horizontally, the Pure HTTP transport can be deployed behind a load balancer. The load balancer can distribute incoming requests across multiple server instances, improving scalability and reliability. HTTP cookies can be used to maintain session affinity or store server state if needed.
The server can also use cookies to link to state stored in a distributed cache (e.g., Redis) or database that all server instances can access.

## Compatibility with Future MCP Versions

This transport specification is designed to stay at the transport layer and should be compatible with future MCP protocol versions.

## Security Implications

This SEP has no additional security implications.

## References

- [HTTP RFCs]: https://httpwg.org/specs/