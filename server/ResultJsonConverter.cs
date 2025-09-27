using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;

public class ResultJsonConverter : JsonConverter<Result>
{
    public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (var doc = JsonDocument.ParseValue(ref reader))
        {
            var root = doc.RootElement;
            // Heuristic: check for unique property to determine type
            if (root.TryGetProperty("action", out _))
            {
                var er = JsonSerializer.Deserialize<ElicitResult>(root.GetRawText(), options);
                if (er != null) return er;
                throw new JsonException("Failed to deserialize ElicitResult.");
            }
            if (root.TryGetProperty("completion", out _))
            {
                var cr = JsonSerializer.Deserialize<CompleteResult>(root.GetRawText(), options);
                if (cr != null) return cr;
                throw new JsonException("Failed to deserialize CompleteResult.");
            }
            if (root.TryGetProperty("roots", out _))
            {
                var lr = JsonSerializer.Deserialize<ListRootsResult>(root.GetRawText(), options);
                if (lr != null) return lr;
                throw new JsonException("Failed to deserialize ListRootsResult.");
            }
            // Fallback: try to deserialize as base Result (for Ping, etc.)
            var baseResult = JsonSerializer.Deserialize<Result>(root.GetRawText(), options);
            if (baseResult != null) return baseResult;
            throw new NotSupportedException($"Unknown result type: could not match any known Result type by property, and base Result deserialization failed.");
        }
    }

    public override void Write(Utf8JsonWriter writer, Result value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
