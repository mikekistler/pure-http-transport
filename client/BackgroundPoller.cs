using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PureHttpTransport.Models;
using PureHttpTransport;
using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace PureHttpMcpClient;

public class ServerRequestEntry(string requestId, IServerRequest request)
{
    public string RequestId { get; } = requestId;
    public IServerRequest Request { get; } = request;
}

public class PendingToolCall(Uri pollUri)
{
    public Uri PollUri { get; } = pollUri;
    public TaskCompletionSource<CallToolResult?> TaskCompletionSource { get; } = new();
}

public class BackgroundPoller : IDisposable
{
    private readonly McpClient _mcpClient;
    private readonly ILogger<BackgroundPoller> _logger;
    private readonly CancellationTokenSource _cts = new();
    private Task? _pollerTask;
    public static readonly ConcurrentQueue<ServerRequestEntry> RequestQueue = new();

    public static readonly ConcurrentDictionary<string, PendingToolCall> OutstandingToolCalls = new();

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
                // Poll /requests endpoint
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

                // Poll /notifications endpoint
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

                // Poll outstanding tool calls
                try
                {
                    foreach (var entry in OutstandingToolCalls.ToArray())
                    {
                        try
                        {
                            // Issue a GET to the PollUri
                            var toolResponse = await _mcpClient.HttpClient.GetAsync(entry.Value.PollUri, _cts.Token);
                            if (toolResponse.StatusCode == System.Net.HttpStatusCode.Accepted)
                            {
                                // Still processing, do nothing
                                _logger.LogInformation($"[Background] Tool call {entry.Key} still processing.");
                            }
                            else if (toolResponse.IsSuccessStatusCode && toolResponse.Content.Headers.ContentLength > 0)
                            {
                                var toolContent = await toolResponse.Content.ReadAsStringAsync(_cts.Token);
                                var callToolResult = JsonSerializer.Deserialize<CallToolResult>(toolContent, serializerOptions);
                                if (callToolResult is not null)
                                {
                                    _logger.LogInformation($"[Background] Tool call {entry.Key} completed with result.");
                                    // Set the result on the TaskCompletionSource
                                    entry.Value.TaskCompletionSource.SetResult(callToolResult);
                                    // Remove from OutstandingToolCalls
                                    OutstandingToolCalls.TryRemove(entry.Key, out _);
                                }
                            }
                            else
                            {
                                // Some error occurred
                                _logger.LogWarning($"[Background] Tool call {entry.Key} returned status {toolResponse.StatusCode}.");
                                // Optionally, you could set an exception on the TaskCompletionSource here
                                entry.Value.TaskCompletionSource.SetException(new Exception($"Tool call returned status {toolResponse.StatusCode}"));
                                OutstandingToolCalls.TryRemove(entry.Key, out _);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"[Background] Error polling tool call {entry.Key}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[Background] Error polling outstanding tool calls: {ex.Message}");
                }

                await Task.Delay(2000, _cts.Token);
            }
        }, _cts.Token);
    }

    // This method accepts the URI for a pending tool call and creates an entry in the OutstandingToolCalls dictionary
    // which the background poller will poll and then complete when the tool call is done.
    // This method does not do the polling itself. That is done in the background task.
    // This method waits on a the result of the polling and returns it to the caller.
    internal static async Task<CallToolResult?> PollPendingToolCallAsync(Uri pollUri)
    {
        var pendingToolCall = new PendingToolCall(pollUri);
        OutstandingToolCalls[pollUri.ToString()] = pendingToolCall;
        return await pendingToolCall.TaskCompletionSource.Task;
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
