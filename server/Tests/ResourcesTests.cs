using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;

namespace PureHttpTransport.Tests
{
    public class ResourcesTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ResourcesTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task AddAndReadResource_Works()
        {
            var client = _factory.CreateClient();
            var add = await client.PostAsJsonAsync("/internal/addResource", new { name = "foo", content = "hello", contentType = "text/plain" });
            add.EnsureSuccessStatusCode();
            var obj = await add.Content.ReadFromJsonAsync<dynamic>();
            string id = obj.id;

            var read = await client.PostAsJsonAsync("/resources", new { id = id });
            read.EnsureSuccessStatusCode();
            var body = await read.Content.ReadFromJsonAsync<dynamic>();
            Assert.Equal("hello", (string)body.content);
            Assert.Equal("text/plain", (string)body.contentType);
        }

        [Fact]
        public async Task SubscribeAndUnsubscribe_Works()
        {
            var client = _factory.CreateClient();
            var add = await client.PostAsJsonAsync("/internal/addResource", new { name = "bar", content = "data" });
            add.EnsureSuccessStatusCode();
            var id = (await add.Content.ReadFromJsonAsync<dynamic>()).id;

            var sub = await client.PostAsJsonAsync("/resources/subscribe", new { id = id });
            Assert.Equal(HttpStatusCode.Accepted, sub.StatusCode);

            var unsub = await client.PostAsJsonAsync("/resources/unsubscribe", new { id = id });
            Assert.Equal(HttpStatusCode.Accepted, unsub.StatusCode);
        }

        [Fact]
        public async Task ListTemplates_ReturnsTemplates()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/resources/templates");
            resp.EnsureSuccessStatusCode();
            var arr = await resp.Content.ReadFromJsonAsync<List<string>>();
            Assert.Contains("default", arr);
        }
    }
}
