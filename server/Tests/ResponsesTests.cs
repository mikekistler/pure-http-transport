using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using System.Net.Http;
using System.Linq;

namespace PureHttpTransport.Tests
{
    public class ResponsesTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ResponsesTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task PostResponses_MissingHeader_ReturnsBadRequest()
        {
            var client = _factory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Post, "/responses");
            var resp = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task PostResponses_EmptyHeader_ReturnsBadRequest()
        {
            var client = _factory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Post, "/responses");
            req.Headers.Add("Mcp-Request-Id", string.Empty);
            var resp = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task PostResponses_NotPending_ReturnsNotFound()
        {
            var client = _factory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Post, "/responses");
            req.Headers.Add("Mcp-Request-Id", System.Guid.NewGuid().ToString());
            var resp = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task PostResponses_AcceptsPendingRequest_ReturnsAccepted()
        {
            var client = _factory.CreateClient();

            // Enqueue and make it pending
            var enqueueResp = await client.PostAsJsonAsync("/internal/enqueueRequest", new { method = "ping", @params = new { } });
            enqueueResp.EnsureSuccessStatusCode();

            var getResp = await client.GetAsync("/requests");
            getResp.EnsureSuccessStatusCode();
            Assert.True(getResp.Headers.Contains("Mcp-Request-Id"));
            var mcpId = getResp.Headers.GetValues("Mcp-Request-Id").First();

            var req = new HttpRequestMessage(HttpMethod.Post, "/responses");
            req.Headers.Add("Mcp-Request-Id", mcpId);
            var resp = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
        }
    }
}
