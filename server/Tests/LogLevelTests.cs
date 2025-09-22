using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using System.Net.Http;
using System.Linq;

namespace PureHttpTransport.Tests
{
    public class LogLevelTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public LogLevelTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task SetValidLogLevel_ReturnsOk()
        {
            var client = _factory.CreateClient();
            var resp = await client.PostAsJsonAsync("/logLevel", new { level = "debug" });
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadFromJsonAsync<dynamic>();
            Assert.Equal("debug", (string)body.level);

            // verify internal getter
            var get = await client.GetAsync("/internal/getLogLevel");
            get.EnsureSuccessStatusCode();
            var getBody = await get.Content.ReadFromJsonAsync<dynamic>();
            Assert.Equal("debug", (string)getBody.level);
        }

        [Fact]
        public async Task SetInvalidLogLevel_ReturnsBadRequest()
        {
            var client = _factory.CreateClient();
            var resp = await client.PostAsJsonAsync("/logLevel", new { level = "loud" });
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task SetEmptyLogLevel_ReturnsBadRequest()
        {
            var client = _factory.CreateClient();
            var resp = await client.PostAsJsonAsync("/logLevel", new { level = "" });
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }
    }
}
