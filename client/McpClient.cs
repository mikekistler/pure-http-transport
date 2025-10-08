using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using PureHttpMcpClient;

namespace PureHttpMcpClient;

public class McpClient
{
    public HttpClient HttpClient => _httpClient;
    private readonly HttpClient _httpClient;
    private readonly ILogger<McpClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _sessionId;

    public McpClient(HttpClient httpClient, ILogger<McpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.Strict
        };
    }

    public async Task<InitializeResult?> InitializeAsync(string clientName = "PureHttpMcpClient", string clientVersion = "1.0.0")
    {
        var initParams = new InitializeRequestParams
        {
            ClientInfo = new Implementation
            {
                Name = clientName,
                Version = clientVersion,
                Title = "Pure HTTP MCP Client"
            },
            ProtocolVersion = "2025-06-18",
            Capabilities = new ClientCapabilities()
        };

        try
        {
            var json = JsonSerializer.Serialize(initParams, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("initialize", content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<InitializeResult>(_jsonOptions);
            if (result != null)
            {
                _sessionId = Guid.NewGuid().ToString();
                _httpClient.DefaultRequestHeaders.Remove("MCP-Session-ID");
                _httpClient.DefaultRequestHeaders.Add("MCP-Session-ID", _sessionId);
                _logger.LogInformation("Initialized session {SessionId}", _sessionId);
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to initialize MCP session");
            throw;
        }
    }

    public async Task<List<Tool>?> ListToolsAsync(string? cursor = null)
    {
        try
        {
            var url = "tools";
            if (!string.IsNullOrEmpty(cursor))
            {
                url += $"?cursor={Uri.EscapeDataString(cursor)}";
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ListToolsResult>(_jsonOptions);
            return result?.Tools?.ToList();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to list tools");
            throw;
        }
    }

    public async Task<CallToolResult?> CallToolAsync(string toolName, IReadOnlyDictionary<string, JsonElement>? arguments = null)
    {
        try
        {
            var requestId = Guid.NewGuid().ToString();
            var requestParams = new CallToolRequestParams
            {
                Name = toolName,
                Arguments = arguments
            };
            var json = JsonSerializer.Serialize(requestParams, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, $"tools/{toolName}/calls")
            {
                Content = content
            };
            // put the requestId in the Mcp-Request-ID header
            request.Headers.Add(PureHttpTransport.PureHttpTransport.McpRequestIdHeader, requestId);
            var response = await _httpClient.SendAsync(request);
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CallToolResult>(_jsonOptions);
            }

            // 202 Accepted, return a PendingToolCall for background polling
            Uri? pollUri = response.Headers.Location ??
                new Uri(_httpClient.BaseAddress!, $"tools/{toolName}/calls/{requestId}");

            // Create a PendingToolCall object and add it to the BackgroundToolCallPoller
            return await BackgroundPoller.PollPendingToolCallAsync(pollUri);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to call tool {ToolName}", toolName);
            throw;
        }
    }

    public async Task<List<Resource>?> ListResourcesAsync(string? cursor = null)
    {
        try
        {
            var url = "resources";
            if (!string.IsNullOrEmpty(cursor))
            {
                url += $"?cursor={Uri.EscapeDataString(cursor)}";
            }

            var response = await _httpClient.GetAsync((global::System.String)"resources");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ListResourcesResult>(_jsonOptions);
            return result?.Resources?.ToList();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to list resources");
            throw;
        }
    }

    public async Task<ReadResourceResult?> ReadResourceAsync(string uri)
    {
        try
        {
            // url encode the uri
            var encodedUri = Uri.EscapeDataString(uri);
            var response = await _httpClient.GetAsync($"resources/{encodedUri}");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ReadResourceResult>(_jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            var message = $"Failed to read resource. Invalid or unreachable URI: {uri}";
            _logger.LogError(ex, message);
            throw new HttpRequestException(message, ex);
        }
    }

    public async Task<List<Prompt>?> ListPromptsAsync(string? cursor = null)
    {
        try
        {
            var url = "prompts";
            if (!string.IsNullOrEmpty(cursor))
            {
                url += $"?cursor={Uri.EscapeDataString(cursor)}";
            }

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ListPromptsResult>(_jsonOptions);
            return result?.Prompts?.ToList();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to list prompts");
            throw;
        }
    }

    public async Task<GetPromptResult?> GetPromptAsync(string name, IReadOnlyDictionary<string, JsonElement>? arguments = null)
    {
        try
        {
            // url encode the name
            var encodedName = Uri.EscapeDataString(name);
            var request = new HttpRequestMessage(HttpMethod.Get, $"prompts/{encodedName}");

            // Put the argumentsHeader in the Mcp-Arguments header
            if (arguments is not null && arguments.Count > 0)
            {
                var argumentsHeader = JsonSerializer.Serialize(arguments, _jsonOptions);
                // remove any newlines from the argumentsHeader
                argumentsHeader = argumentsHeader.Replace("\n", "").Replace("\r", "");
                request.Headers.Add("Mcp-Arguments", argumentsHeader);
            }

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<GetPromptResult>(_jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get prompt {Name}", name);
            throw;
        }
    }

    public async Task<bool> PingAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("ping");
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ping failed");
            return false;
        }
    }

    public async Task<bool> SubscribeResourceAsync(string uri)
    {
        try
        {
            var requestParams = new { Uri = uri };
            var json = JsonSerializer.Serialize(requestParams, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("resources/subscribe", content);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to subscribe to resource {Uri}", uri);
            return false;
        }
    }

    public async Task<bool> UnsubscribeResourceAsync(string uri)
    {
        try
        {
            var requestParams = new { Uri = uri };
            var json = JsonSerializer.Serialize(requestParams, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("resources/unsubscribe", content);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe to resource {Uri}", uri);
            return false;
        }
    }

    public async Task SendElicitResultAsync(string requestId, ElicitResult result)
    {
        try
        {
            var json = JsonSerializer.Serialize(result, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, "responses")
            {
                Content = content
            };
            // put the requestId in the Mcp-Request-ID header
            request.Headers.Add(PureHttpTransport.PureHttpTransport.McpRequestIdHeader, requestId);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed attempting to send elicit result for request {RequestId}", requestId);
        }
    }
}
