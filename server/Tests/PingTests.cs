using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PureHttpTransport.Tests
{
    public class PingTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public PingTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Ping_ReturnsAccepted()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/ping");
            Assert.Equal(HttpStatusCode.Accepted, resp.StatusCode);
        }
    }
}
