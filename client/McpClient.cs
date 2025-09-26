using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace PureHttpMcpClient;

public class McpClient
{
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
            WriteIndented = true
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
            var response = await _httpClient.PostAsJsonAsync("initialize", initParams, _jsonOptions);
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
            var requestParams = new CallToolRequestParams
            {
                Name = toolName,
                Arguments = arguments
            };

            var response = await _httpClient.PostAsJsonAsync($"tools/{toolName}/calls", requestParams, _jsonOptions);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<CallToolResult>(_jsonOptions);
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

            var response = await _httpClient.GetAsync(url);
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
            var requestParams = new ReadResourceRequestParams
            {
                Uri = uri
            };

            var response = await _httpClient.PostAsJsonAsync("resources", requestParams, _jsonOptions);
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
            var requestParams = new GetPromptRequestParams
            {
                Name = name,
                Arguments = arguments
            };

            var response = await _httpClient.PostAsJsonAsync($"prompts/{name}", requestParams, _jsonOptions);
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
            var response = await _httpClient.PostAsync("ping", null);
            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ping failed");
            return false;
        }
    }
}