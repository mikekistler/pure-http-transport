using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using PureHttpTransport;

namespace PureHttpTransport.Tests
{
    public class RequestsLifecycleTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public RequestsLifecycleTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ActiveToPendingToCompletedFlow()
        {
            var client = _factory.CreateClient();

            // Enqueue
            var enqueueResp = await client.PostAsJsonAsync("/internal/enqueueRequest", new { method = "ping", @params = new { } });
            enqueueResp.EnsureSuccessStatusCode();
            var idObj = await enqueueResp.Content.ReadFromJsonAsync<dynamic>();
            string id = idObj.id;

            // Get request -> should be 200 and header present
            var getResp = await client.GetAsync("/requests");
            getResp.EnsureSuccessStatusCode();
            Assert.True(getResp.Headers.Contains("Mcp-Request-Id"));
            var mcpId = getResp.Headers.GetValues("Mcp-Request-Id").First();

            // Respond to complete
            var request = new HttpRequestMessage(HttpMethod.Post, "/responses");
            request.Headers.Add("Mcp-Request-Id", mcpId);
            var resp = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);

            // Subsequent GET should be NoContent
            var get2 = await client.GetAsync("/requests");
            Assert.Equal(HttpStatusCode.NoContent, get2.StatusCode);
        }

        [Fact]
        public async Task PendingReactivationAfterTimeout()
        {
            var client = _factory.CreateClient();

            // Shorten timeout for test
            RequestsEndpoints.PendingTimeoutMilliseconds = 500; // 0.5s

            var enqueueResp = await client.PostAsJsonAsync("/internal/enqueueRequest", new { method = "ping", @params = new { } });
            enqueueResp.EnsureSuccessStatusCode();

            var getResp = await client.GetAsync("/requests");
            getResp.EnsureSuccessStatusCode();
            var mcpId = getResp.Headers.GetValues("Mcp-Request-Id").First();

            // Do not post response. Force reactivation
            RequestsEndpoints.ForceReactivatePending();

            // Now GET should return the request again
            var get2 = await client.GetAsync("/requests");
            get2.EnsureSuccessStatusCode();
            var mcpId2 = get2.Headers.GetValues("Mcp-Request-Id").First();
            Assert.NotEqual(mcpId, mcpId2);
        }
    }
}
