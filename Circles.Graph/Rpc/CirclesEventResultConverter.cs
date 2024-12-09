using System.Text.Json;
using System.Text.Json.Serialization;
using Circles.Graph.Events;

namespace Circles.Graph.Rpc;

public class CirclesEventResultConverter : JsonConverter<CirclesEventResult>
{
    public override CirclesEventResult? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var root = jsonDoc.RootElement;

        if (!root.TryGetProperty("event", out var eventElement))
        {
            throw new JsonException("Missing 'event' property.");
        }

        var eventType = eventElement.GetString();

        if (!root.TryGetProperty("values", out var valuesElement))
        {
            throw new JsonException("Missing 'values' property.");
        }

        EventValuesBase? values = eventType switch
        {
            "CrcV2_Trust" => valuesElement.Deserialize<TrustEventValues>(options),
            "CrcV2_TransferSingle" => valuesElement.Deserialize<TransferEventValues>(options),
            // Add other event types as needed
            _ => null
        };

        return new CirclesEventResult
        {
            Event = eventType ?? string.Empty,
            Values = values
        };
    }

    public override void Write(Utf8JsonWriter writer, CirclesEventResult value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("event", value.Event);
        writer.WritePropertyName("values");
        JsonSerializer.Serialize(writer, value.Values, value.Values.GetType(), options);
        writer.WriteEndObject();
    }
}