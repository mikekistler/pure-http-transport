using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;

namespace PureHttpTransport.Tests
{
    public class NotificationsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public NotificationsTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetNotifications_ReturnsEmptyArrayWhenNoNotifications()
        {
            var client = _factory.CreateClient();
            var resp = await client.GetAsync("/notifications");
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadFromJsonAsync<List<object>>();
            Assert.NotNull(body);
            Assert.Empty(body);
        }

        [Fact]
        public async Task EnqueueAndGetNotifications_GroupHeaderPresent()
        {
            var client = _factory.CreateClient();
            var items = new List<object> { new { type = "progress", data = "50%" }, new { type = "roots/list_changed", data = new { rootId = "r1" } } };
            var post = await client.PostAsJsonAsync("/internal/enqueueNotifications", items);
            post.EnsureSuccessStatusCode();
            var idObj = await post.Content.ReadFromJsonAsync<dynamic>();
            string id = idObj.id;

            var get = await client.GetAsync("/notifications");
            get.EnsureSuccessStatusCode();
            Assert.True(get.Headers.Contains("Mcp-Notifications-Group-Id"));
            var groupId = get.Headers.GetValues("Mcp-Notifications-Group-Id").First();
            Assert.Equal(id, groupId);

            var body = await get.Content.ReadFromJsonAsync<List<object>>();
            Assert.NotNull(body);
            Assert.Equal(2, body.Count);
        }

        [Fact]
        public async Task AcknowledgeNotifications_MissingHeader_ReturnsBadRequest()
        {
            var client = _factory.CreateClient();
            var req = new HttpRequestMessage(HttpMethod.Post, "/notifications");
            var resp = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task AcknowledgeNotifications_AcceptsPending_ReturnsAccepted()
        {
            var client = _factory.CreateClient();
            var items = new List<object> { new { type = "progress", data = "done" } };
            var post = await client.PostAsJsonAsync("/internal/enqueueNotifications", items);
            post.EnsureSuccessStatusCode();
            var idObj = await post.Content.ReadFromJsonAsync<dynamic>();
            string id = idObj.id;

            var get = await client.GetAsync("/notifications");
            get.EnsureSuccessStatusCode();
            var groupId = get.Headers.GetValues("Mcp-Notifications-Group-Id").First();

            var acknowledge = new HttpRequestMessage(HttpMethod.Post, "/notifications");
            acknowledge.Headers.Add("Mcp-Notifications-Group-Id", groupId);
            var ackResp = await client.SendAsync(acknowledge);
            Assert.Equal(HttpStatusCode.Accepted, ackResp.StatusCode);
        }

        [Fact]
        public async Task PendingReactivation_AllowsRedelivery()
        {
            var client = _factory.CreateClient();
            NotificationsEndpoints.PendingTimeoutMilliseconds = 500;

            var items = new List<object> { new { type = "progress", data = "half" } };
            var post = await client.PostAsJsonAsync("/internal/enqueueNotifications", items);
            post.EnsureSuccessStatusCode();

            var get = await client.GetAsync("/notifications");
            get.EnsureSuccessStatusCode();
            var groupId = get.Headers.GetValues("Mcp-Notifications-Group-Id").First();

            // Force reactivation
            NotificationsEndpoints.ForceReactivatePendingNotifications();

            var get2 = await client.GetAsync("/notifications");
            get2.EnsureSuccessStatusCode();
            var groupId2 = get2.Headers.GetValues("Mcp-Notifications-Group-Id").First();

            Assert.NotEqual(groupId, groupId2);
        }
    }
}
