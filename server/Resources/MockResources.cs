using ModelContextProtocol.Protocol;
using System.ComponentModel;

namespace PureHttpMcpServer.Resources;

public static class MockResources
{
    private static List<Resource> _resources = [
        new Resource()
        {
            Uri = "test://static/resource/1",
            Name = "Static Resource 1",
            MimeType = "text/plain",
            Description = "This is a static plaintext resource"
        }
    ];

    private static List<ResourceTemplate> _resourceTemplates = [
        new ResourceTemplate()
        {
            UriTemplate = "test://text/resource/{id}",
            Name = "Template Text Resource",
            Description = "A template resource with text content",
            MimeType = "text/plain"
        },
        new ResourceTemplate()
        {
            UriTemplate = "test://blob/resource/{id}",
            Name = "Template Blob Resource",
            Description = "A template resource with blob content",
            MimeType = "application/octet-stream"
        }
    ];

    public static IEnumerable<Resource> ListResources()
    {
        return _resources;
    }

    public static IEnumerable<ResourceTemplate> ListResourceTemplates()
    {
        return _resourceTemplates;
    }

    public static Resource? GetResource(string uri)
    {
        return _resources.FirstOrDefault(r => r.Uri == uri);
    }

    public static ResourceTemplate? GetResourceTemplate(string uri)
    {
        return _resourceTemplates.FirstOrDefault(t => uri.StartsWith(t.UriTemplate.Split('{')[0]));
    }

    public static List<ResourceContents>? GetResourceContents(string uri)
    {
        // First check static resources
        var resource = _resources.FirstOrDefault(r => r.Uri == uri);
        var resourceTemplate = _resourceTemplates.FirstOrDefault(t => uri.StartsWith(t.UriTemplate.Split('{')[0]));

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
                Uri = uri,
                MimeType = mimeType,
                Text = description,
            });
        }
        else
        {
            result.Add(new BlobResourceContents
            {
                Uri = uri,
                MimeType = mimeType,
                Blob = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(description)),
            });
        }

        return result;
    }
}
