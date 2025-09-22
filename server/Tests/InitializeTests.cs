using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PureHttpTransport.Tests
{
    public class InitializeTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public InitializeTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Initialize_Returns200AndSessionHeader()
        {
            var client = _factory.CreateClient();

            var body = new
            {
                capabilities = new { },
                clientInfo = new { name = "test-client", version = "1.0" },
                protocolVersion = "2025-06-18"
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await client.PostAsync("/initialize", content);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.True(resp.Headers.Contains("Mcp-Session-Id"), "Response should include Mcp-Session-Id header");

            var respJson = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(respJson);
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("protocolVersion", out var pv));
            Assert.Equal("2025-06-18", pv.GetString());

            Assert.True(root.TryGetProperty("serverInfo", out var si));
            Assert.True(si.TryGetProperty("name", out var name));
            Assert.Equal("PureHttpMcpServer", name.GetString());
        }
    }
}
