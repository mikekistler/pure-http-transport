using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PureHttpMcpClient;

namespace PureHttpMcpClient
{
    public class BackgroundPoller : IDisposable
    {
        private readonly McpClient _mcpClient;
        private readonly CancellationTokenSource _cts = new();
        private Task? _pollerTask;

        public BackgroundPoller(McpClient mcpClient)
        {
            _mcpClient = mcpClient;
        }

        public void Start()
        {
            _pollerTask = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var reqResponse = await _mcpClient.HttpClient.GetAsync("requests", _cts.Token);
                        if (reqResponse.IsSuccessStatusCode && reqResponse.Content.Headers.ContentLength > 0)
                        {
                            var reqContent = await reqResponse.Content.ReadAsStringAsync();
                            if (!string.IsNullOrWhiteSpace(reqContent) && reqContent != "{}" && reqContent != "[]")
                            {
                                Console.WriteLine("\n[Background] /requests response:");
                                Console.WriteLine(reqContent);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _ = ex;
                    }
                    try
                    {
                        var notifResponse = await _mcpClient.HttpClient.GetAsync("notifications", _cts.Token);
                        if (notifResponse.IsSuccessStatusCode && notifResponse.Content.Headers.ContentLength > 0)
                        {
                            var notifContent = await notifResponse.Content.ReadAsStringAsync();
                            if (!string.IsNullOrWhiteSpace(notifContent) && notifContent != "{}" && notifContent != "[]")
                            {
                                Console.WriteLine("\n[Background] /notifications response:");
                                Console.WriteLine(notifContent);
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
                                        Console.WriteLine($"[Background] Successfully acknowledged notifications with Group ID: {groupId}");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"[Background] Failed to acknowledge notifications with Group ID: {groupId}. Status: {ackResponse.StatusCode}");
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
}
