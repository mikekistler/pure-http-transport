using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using System.Net.Http;
using System.Linq;

namespace PureHttpTransport.Tests
{
    public class CompletionsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public CompletionsTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task PostCompletions_ReturnsOkAndResult()
        {
            var client = _factory.CreateClient();
            var resp = await client.PostAsJsonAsync("/completions", new { prompt = "Say hello" });
            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadFromJsonAsync<dynamic>();
            Assert.NotNull(body.id);
            Assert.NotNull(body.model);
            Assert.NotNull(body.choices);
            Assert.Equal("Hello world", (string)body.choices[0].text);
        }

        [Fact]
        public async Task PostCompletions_MissingBody_ReturnsBadRequest()
        {
            var client = _factory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Post, "/completions");
            var resp = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }
    }
}
