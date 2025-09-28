using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PureHttpTransport.Models;
using PureHttpTransport;

using System.Text.Json;

namespace PureHttpMcpClient;

public class ServerRequestEntry(string requestId, IServerRequest request)
{
    public string RequestId { get; } = requestId;
    public IServerRequest Request { get; } = request;
}

public class BackgroundPoller : IDisposable
{
    private readonly McpClient _mcpClient;
    private readonly ILogger<BackgroundPoller> _logger;
    private readonly CancellationTokenSource _cts = new();
    private Task? _pollerTask;
    public static readonly ConcurrentQueue<ServerRequestEntry> RequestQueue = new();

    public BackgroundPoller(McpClient mcpClient, ILogger<BackgroundPoller> logger)
    {
        _mcpClient = mcpClient;
        _logger = logger;
    }

    public void Start()
    {
        JsonSerializerOptions serializerOptions = JsonSerializerOptions.Web;

        _pollerTask = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var reqResponse = await _mcpClient.HttpClient.GetAsync("requests", _cts.Token);

                    if (reqResponse.IsSuccessStatusCode && reqResponse.Content.Headers.ContentLength > 0)
                    {
                        string? requestId = null;
                        IServerRequest? serverRequest = null;

                        // Try to get requestId from header or content
                        if (reqResponse.Headers.TryGetValues(PureHttpTransport.PureHttpTransport.McpRequestIdHeader, out var ids))
                        {
                            requestId = ids.FirstOrDefault();
                        }

                        if (string.IsNullOrEmpty(requestId))
                        {
                            // This is an error. Issue a log message
                            _logger.LogWarning("Received /requests response without a valid Mcp-Request-Id header.");
                            continue;
                        }

                        var reqContent = await reqResponse.Content.ReadAsStringAsync();
                        // Deserialize to ServerRequest or its concrete subclass
                        serverRequest = JsonSerializer.Deserialize<IServerRequest>(reqContent, serializerOptions);

                        if (serverRequest is not null && !string.IsNullOrEmpty(requestId))
                        {
                            _logger.LogInformation("Received server request with RequestId {RequestId} of type {Type} ",
                                requestId, serverRequest.GetType().Name);
                            RequestQueue.Enqueue(new ServerRequestEntry(requestId, serverRequest));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[Background] Error fetching /requests: {ex.Message}");
                }

                try
                {
                    var notifResponse = await _mcpClient.HttpClient.GetAsync("notifications", _cts.Token);
                    if (notifResponse.IsSuccessStatusCode && notifResponse.Content.Headers.ContentLength > 0)
                    {
                        var notifContent = await notifResponse.Content.ReadAsStringAsync();
                        if (!string.IsNullOrWhiteSpace(notifContent) && notifContent != "{}" && notifContent != "[]")
                        {
                            _logger.LogInformation("\n[Background] /notifications response:");
                            _logger.LogInformation(notifContent);
                        }

                        // Acknowledge notifications
                        // Get the Mcp-Group-ID header from the response
                        if (notifResponse.Headers.TryGetValues("MCP-Group-ID", out var groupIds))
                        {
                            var groupId = groupIds.FirstOrDefault();
                            if (!string.IsNullOrEmpty(groupId))
                            {
                                // sent the groupId back in the MCP-Group-ID header to acknowledge
                                var request = new HttpRequestMessage(HttpMethod.Post, $"notifications");
                                request.Headers.Add("Mcp-Group-Id", groupId);
                                request.Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json");
                                var ackResponse = await _mcpClient.HttpClient.SendAsync(request, _cts.Token);
                                if (ackResponse.IsSuccessStatusCode)
                                {
                                    _logger.LogInformation($"[Background] Successfully acknowledged notifications with Group ID: {groupId}");
                                }
                                else
                                {
                                    _logger.LogWarning($"[Background] Failed to acknowledge notifications with Group ID: {groupId}. Status: {ackResponse.StatusCode}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _ = ex;
                }
                await Task.Delay(2000, _cts.Token);
            }
        }, _cts.Token);
    }

    public async Task StopAsync()
    {
        _cts.Cancel();
        if (_pollerTask != null)
        {
            try { await _pollerTask; } catch { }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
