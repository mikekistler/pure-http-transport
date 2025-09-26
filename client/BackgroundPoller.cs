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
