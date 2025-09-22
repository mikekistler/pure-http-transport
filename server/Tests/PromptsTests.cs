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
    public class PromptsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public PromptsTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ListPrompts_ReturnsAvailableNames()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/prompts");
            resp.EnsureSuccessStatusCode();
            var list = await resp.Content.ReadFromJsonAsync<List<string>>();
            Assert.NotNull(list);
            Assert.Contains("greeting", list);
        }

        [Fact]
        public async Task GetPrompt_RendersWithParams()
        {
            var client = _factory.CreateClient();
            var body = new Dictionary<string, object> { ["name"] = "Alice" };
            var resp = await client.PostAsJsonAsync("/prompts/greeting", body);
            resp.EnsureSuccessStatusCode();
            var obj = await resp.Content.ReadFromJsonAsync<dynamic>();
            Assert.Equal("Hello, Alice!", (string)obj.text);
        }

        [Fact]
        public async Task SetPrompt_And_GetPrompt_Works()
        {
            var client = _factory.CreateClient();
            var set = await client.PostAsJsonAsync("/internal/setPrompt/custom", "Welcome {{user}}");
            set.EnsureSuccessStatusCode();

            var body = new Dictionary<string, object> { ["user"] = "Bob" };
            var resp = await client.PostAsJsonAsync("/prompts/custom", body);
            resp.EnsureSuccessStatusCode();
            var obj = await resp.Content.ReadFromJsonAsync<dynamic>();
            Assert.Equal("Welcome Bob", (string)obj.text);
        }
    }
}
