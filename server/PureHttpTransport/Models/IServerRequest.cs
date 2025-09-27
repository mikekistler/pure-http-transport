using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;

namespace PureHttpTransport.Models;

/// <summary>
/// Base type for all server requests.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "Method")]
[JsonDerivedType(typeof(PingRequest), RequestMethods.Ping)]
[JsonDerivedType(typeof(CreateMessageRequest), RequestMethods.SamplingCreateMessage)]
[JsonDerivedType(typeof(ListRootsRequest), RequestMethods.RootsList)]
[JsonDerivedType(typeof(ElicitRequest), RequestMethods.ElicitationCreate)]
public interface IServerRequest { }

public class PingRequest : IServerRequest
{
    public string Method => RequestMethods.Ping;
}

public class CreateMessageRequest(CreateMessageRequestParams requestParams) : IServerRequest
{
    public string Method => RequestMethods.SamplingCreateMessage;
    public CreateMessageRequestParams Params { get; set; } = requestParams;
}

public class ListRootsRequest(ListResourcesRequestParams requestParams) : IServerRequest
{
    public string Method => RequestMethods.RootsList;
    public ListResourcesRequestParams Params { get; set; } = requestParams;
}

public class ElicitRequest(ElicitRequestParams requestParams) : IServerRequest
{
    public string Method => RequestMethods.ElicitationCreate;
    public ElicitRequestParams Params { get; set; } = requestParams;
}