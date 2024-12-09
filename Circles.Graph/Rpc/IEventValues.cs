using System.Text.Json.Serialization;

namespace Circles.Graph.Events;

public abstract class EventValuesBase
{
    [JsonPropertyName("blockNumber")] public string BlockNumber { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")] public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("logIndex")] public string LogIndex { get; set; } = string.Empty;

    [JsonPropertyName("transactionIndex")] public string? TransactionIndex { get; set; }

    [JsonPropertyName("transactionHash")] public string? TransactionHash { get; set; }

    public long BlockNumberLong => ConvertHexToLong(BlockNumber);
    public long TimestampLong => ConvertHexToLong(Timestamp);
    public int LogIndexInt => (int)ConvertHexToLong(LogIndex);
    public int TransactionIndexInt => (int)ConvertHexToLong(TransactionIndex);

    protected static long ConvertHexToLong(string? hex)
    {
        if (string.IsNullOrEmpty(hex)) return 0;
        if (hex.StartsWith("0x")) hex = hex[2..];
        return Convert.ToInt64(hex, 16);
    }
}