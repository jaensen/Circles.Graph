using System.Numerics;
using System.Text.Json.Serialization;
using Circles.Graph.Events;

namespace Circles.Graph.Rpc;

public class TrustEventValues : EventValuesBase
{
    [JsonPropertyName("truster")] public string? Truster { get; set; }

    [JsonPropertyName("trustee")] public string? Trustee { get; set; }

    [JsonPropertyName("expiryTime")] public string? ExpiryTime { get; set; }

    public BigInteger ExpiryTimeBigInteger => BigInteger.Parse(ExpiryTime);
}