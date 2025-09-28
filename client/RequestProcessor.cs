using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using PureHttpTransport.Models;

namespace PureHttpMcpClient;

public class RequestProcessor
{
    private readonly McpClient _mcpClient;

    public RequestProcessor(McpClient mcpClient)
    {
        _mcpClient = mcpClient;
    }

    public void ProcessRequests()
    {
        while (BackgroundPoller.RequestQueue.TryDequeue(out var serverRequestEntry))
        {
            if (serverRequestEntry.Request is ElicitRequest elicitRequest)
            {
                var result = HandleElicitationSync(elicitRequest.Params);

                // use _mcpClient here to send the result back if needed
                _mcpClient.SendElicitResult(serverRequestEntry.RequestId, result);
            }
        }
    }

    public ElicitResult HandleElicitationSync(ElicitRequestParams? requestParams)
    {
        if (requestParams?.RequestedSchema?.Properties == null)
        {
            return new ElicitResult();
        }

        if (requestParams?.Message is not null)
        {
            Console.WriteLine(requestParams.Message);
        }

        var content = new Dictionary<string, JsonElement>();

        foreach (var property in requestParams!.RequestedSchema.Properties)
        {
            if (property.Value is ElicitRequestParams.BooleanSchema booleanSchema)
            {
                Console.Write($"{booleanSchema.Description}: ");
                var clientInput = Console.ReadLine();
                bool parsedBool;

                if (bool.TryParse(clientInput, out parsedBool))
                {
                    content[property.Key] = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(parsedBool));
                }
                else if (string.Equals(clientInput?.Trim(), "yes", StringComparison.OrdinalIgnoreCase))
                {
                    content[property.Key] = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(true));
                }
                else if (string.Equals(clientInput?.Trim(), "no", StringComparison.OrdinalIgnoreCase))
                {
                    content[property.Key] = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(false));
                }
            }
            else if (property.Value is ElicitRequestParams.NumberSchema numberSchema)
            {
                Console.Write($"{numberSchema.Description}: ");
                var clientInput = Console.ReadLine();
                double parsedNumber;
                if (double.TryParse(clientInput, out parsedNumber))
                {
                    content[property.Key] = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(parsedNumber));
                }
            }
            else if (property.Value is ElicitRequestParams.StringSchema stringSchema)
            {
                Console.Write($"{stringSchema.Description}: ");
                var clientInput = Console.ReadLine();
                content[property.Key] = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(clientInput));
            }
        }

        return new ElicitResult
        {
            Action = "accept",
            Content = content
        };
    }
}
