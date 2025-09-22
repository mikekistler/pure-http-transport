using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PureHttpTransport.Tests
{
    public class RequestsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public RequestsTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetRequests_NoContentWhenEmpty()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/requests");
            Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        }

        [Fact]
        public async Task GetRequests_ReturnsEnqueuedPingRequest()
        {
            var client = _factory.CreateClient();
            var ping = new { method = "ping", @params = new { } };
            var post = await client.PostAsJsonAsync("/internal/enqueueRequest", ping);
            post.EnsureSuccessStatusCode();

            var resp = await client.GetAsync("/requests");
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadFromJsonAsync<object>();
            Assert.NotNull(body);
        }

        [Fact]
        public async Task GetRequests_ReturnsEnqueuedElicitRequest()
        {
            var client = _factory.CreateClient();
            var elicit = new { method = "elicitation/create", @params = new { message = "Please confirm" } };
            var post = await client.PostAsJsonAsync("/internal/enqueueRequest", elicit);
            post.EnsureSuccessStatusCode();

            var resp = await client.GetAsync("/requests");
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadFromJsonAsync<object>();
            Assert.NotNull(body);
        }
    }
}
