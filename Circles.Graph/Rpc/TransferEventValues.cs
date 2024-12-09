using System.Numerics;
using System.Text.Json.Serialization;

namespace Circles.Graph.Events;

public class TransferEventValues : EventValuesBase
{
    [JsonPropertyName("batchIndex")]
    public string? BatchIndex { get; set; }
    public long BatchIndexLong => ConvertHexToLong(BatchIndex);

    [JsonPropertyName("operator")]
    public string? Operator { get; set; }

    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("to")]
    public string? To { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    public BigInteger ValueBigInteger => BigInteger.Parse(Value);

    public BigInteger IdBigInteger => BigInteger.Parse(Id);

    [JsonPropertyName("tokenAddress")]
    public string? TokenAddress { get; set; }
}