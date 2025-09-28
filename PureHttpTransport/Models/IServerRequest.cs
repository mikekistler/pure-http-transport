using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;

namespace PureHttpTransport.Models;

/// <summary>
/// Base type for all server requests.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "method")]
[JsonDerivedType(typeof(PingRequest), RequestMethods.Ping)]
[JsonDerivedType(typeof(CreateMessageRequest), RequestMethods.SamplingCreateMessage)]
[JsonDerivedType(typeof(ListRootsRequest), RequestMethods.RootsList)]
[JsonDerivedType(typeof(ElicitRequest), RequestMethods.ElicitationCreate)]
public interface IServerRequest { }

public class PingRequest : IServerRequest
{
}

public class CreateMessageRequest(CreateMessageRequestParams @params) : IServerRequest
{
    [JsonPropertyName("params")]
    public CreateMessageRequestParams Params { get; set; } = @params;
}

public class ListRootsRequest(ListResourcesRequestParams @params) : IServerRequest
{
    [JsonPropertyName("params")]
    public ListResourcesRequestParams Params { get; set; } = @params;
}

public class ElicitRequest(ElicitRequestParams @params) : IServerRequest
{
    [JsonPropertyName("params")]
    public ElicitRequestParams Params { get; set; } = @params;
}
