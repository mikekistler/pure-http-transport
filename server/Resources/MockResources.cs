using ModelContextProtocol.Protocol;
using PureHttpTransport;
using System.ComponentModel;
using System.Text.Json.Nodes;

namespace PureHttpMcpServer.Resources;

public class MockResources : IMockResources
{
    // Track subscriptions: resource URI -> set of subscribers (for demo, just a HashSet)
    private static readonly HashSet<string> _subscribedUris = new();

    private static List<Resource> _resources = [
        new Resource()
        {
            Uri = "test://static/resource/1",
            Name = "Static Resource 1",
            MimeType = "text/plain",
            Description = "This is a static plaintext resource",
            Meta = new JsonObject()
        }
    ];

    private static List<ResourceTemplate> _resourceTemplates = [
        new ResourceTemplate()
        {
            UriTemplate = "test://text/resource/{id}",
            Name = "Template Text Resource",
            Description = "A template resource with text content",
            MimeType = "text/plain",
            Meta = new JsonObject()
        },
        new ResourceTemplate()
        {
            UriTemplate = "test://blob/resource/{id}",
            Name = "Template Blob Resource",
            Description = "A template resource with blob content",
            MimeType = "application/octet-stream",
            Meta = new JsonObject()
        }
    ];

    public IEnumerable<Resource> ListResources(ListResourcesRequestParams requestParams)
    {
        return _resources;
    }

    public IEnumerable<ResourceTemplate> ListResourceTemplates(ListResourceTemplatesRequestParams requestParams)
    {
        return _resourceTemplates;
    }

    public bool SubscribeToResource(SubscribeRequestParams requestParams)
    {
        lock (_subscribedUris)
        {
            return _subscribedUris.Add(requestParams.Uri!); // returns true if newly subscribed
        }
    }

    public bool UnsubscribeToResource(UnsubscribeRequestParams requestParams)
    {
        lock (_subscribedUris)
        {
            return _subscribedUris.Remove(requestParams.Uri!); // returns true if was subscribed
        }
    }

    internal static HashSet<string> Subscriptions()
    {
        lock (_subscribedUris)
        {
            return (HashSet<string>)_subscribedUris.ToHashSet();
        }
    }

    public List<ResourceContents>? ReadResource(ReadResourceRequestParams requestParams)
    {
        // First check static resources
        var resource = _resources.FirstOrDefault(r => r.Uri == requestParams.Uri);
        var resourceTemplate = _resourceTemplates.FirstOrDefault(t => requestParams.Uri.StartsWith(t.UriTemplate.Split('{')[0]));

        if (resource == null && resourceTemplate == null)
        {
            return null;
        }

        var mimeType = resource?.MimeType ?? resourceTemplate?.MimeType ?? "text/plain";
        var description = resource?.Description ?? resourceTemplate?.Description ?? "No description";

        var result = new List<ResourceContents>();

        if (mimeType == "text/plain")
        {
            result.Add(new TextResourceContents
            {
                Uri = requestParams.Uri,
                MimeType = mimeType,
                Text = description,
            });
        }
        else
        {
            result.Add(new BlobResourceContents
            {
                Uri = requestParams.Uri,
                MimeType = mimeType,
                Blob = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(description)),
            });
        }

        return result;
    }
}
