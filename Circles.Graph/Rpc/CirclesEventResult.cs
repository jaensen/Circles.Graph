using System.Text.Json.Serialization;
using Circles.Graph.Events;

namespace Circles.Graph.Rpc;

[JsonConverter(typeof(CirclesEventResultConverter))]
public class CirclesEventResult
{
    public string Event { get; set; } = string.Empty;

    public EventValuesBase? Values { get; set; }
}