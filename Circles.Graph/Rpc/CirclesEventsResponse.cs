namespace Circles.Graph.Rpc;

using System.Text.Json.Serialization;

public class CirclesEventsResponse
{
    [JsonPropertyName("jsonrpc")] public string JsonRpc { get; set; } = "";

    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("result")] public List<CirclesEventResult>? Result { get; set; }
}