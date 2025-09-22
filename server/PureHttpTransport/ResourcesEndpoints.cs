using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

namespace PureHttpTransport;

public static class ResourcesEndpoints
{
    private class ResourceItem
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Name { get; init; } = string.Empty;
        public string ContentType { get; init; } = "text/plain";
        public byte[] Data { get; init; } = Array.Empty<byte>();
    }

    private static readonly ConcurrentDictionary<string, ResourceItem> _resources = new();
    private static readonly ConcurrentDictionary<string, bool> _subscriptions = new();

    private static readonly string[] _templates = new[] { "default", "summary", "detailed" };

    public static IEndpointRouteBuilder MapResourcesEndpoints(this IEndpointRouteBuilder app)
    {
        // List resources
        app.MapGet("/resources", () =>
        {
            var list = _resources.Values.Select(r => new { id = r.Id, name = r.Name, contentType = r.ContentType }).ToArray();
            return Results.Json(list);
        })
        .WithName("ListResources");

        // Read a resource
        app.MapPost("/resources", async (HttpRequest req) =>
        {
            // Expect body like { id: "..." }
            Dictionary<string, object>? body = null;
            try
            {
                body = await req.ReadFromJsonAsync<Dictionary<string, object>>();
            }
            catch
            {
                // ignore
            }

            if (body == null || !body.TryGetValue("id", out var idObj) || idObj == null)
            {
                return Results.BadRequest(new { error = "Missing id" });
            }

            var id = idObj.ToString()!;
            if (!_resources.TryGetValue(id, out var resource))
            {
                return Results.NotFound(new { error = "resource not found" });
            }

            // Return JSON with content as string (assume utf8 for tests)
            var content = Encoding.UTF8.GetString(resource.Data);
            return Results.Json(new { id = resource.Id, name = resource.Name, contentType = resource.ContentType, content = content });
        })
        .WithName("ReadResource");

        // Subscribe to a resource
        app.MapPost("/resources/subscribe", async (HttpRequest req) =>
        {
            Dictionary<string, object>? body = null;
            try
            {
                body = await req.ReadFromJsonAsync<Dictionary<string, object>>();
            }
            catch
            {
            }

            if (body == null || !body.TryGetValue("id", out var idObj) || idObj == null)
            {
                return Results.BadRequest(new { error = "Missing id" });
            }

            var id = idObj.ToString()!;
            if (!_resources.ContainsKey(id))
            {
                return Results.NotFound(new { error = "resource not found" });
            }

            _subscriptions[id] = true;
            return Results.Accepted();
        })
        .WithName("SubscribeResource");

        // Unsubscribe
        app.MapPost("/resources/unsubscribe", async (HttpRequest req) =>
        {
            Dictionary<string, object>? body = null;
            try
            {
                body = await req.ReadFromJsonAsync<Dictionary<string, object>>();
            }
            catch
            {
            }

            if (body == null || !body.TryGetValue("id", out var idObj) || idObj == null)
            {
                return Results.BadRequest(new { error = "Missing id" });
            }

            var id = idObj.ToString()!;
            if (_subscriptions.TryRemove(id, out var _))
            {
                return Results.Accepted();
            }
            else
            {
                return Results.NotFound(new { error = "subscription not found" });
            }
        })
        .WithName("UnsubscribeResource");

        // List templates
        app.MapGet("/resources/templates", () => Results.Json(_templates))
            .WithName("ListResourceTemplates");

        // Internal helper to add a resource for tests
        app.MapPost("/internal/addResource", async (HttpRequest req) =>
        {
            Dictionary<string, object>? body = null;
            try
            {
                body = await req.ReadFromJsonAsync<Dictionary<string, object>>();
            }
            catch
            {
            }

            var name = body != null && body.TryGetValue("name", out var n) ? n?.ToString() ?? string.Empty : string.Empty;
            var content = body != null && body.TryGetValue("content", out var c) ? c?.ToString() ?? string.Empty : string.Empty;
            var contentType = body != null && body.TryGetValue("contentType", out var ct) ? ct?.ToString() ?? "text/plain" : "text/plain";

            var resource = new ResourceItem { Name = name, ContentType = contentType, Data = Encoding.UTF8.GetBytes(content) };
            _resources[resource.Id] = resource;
            return Results.Ok(new { id = resource.Id });
        })
        .WithName("AddResource");

        return app;
    }
}
