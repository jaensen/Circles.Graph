namespace Circles.Graph;

public record EventKey(long BlockNumber, long Timestamp, long TransactionIndex, long LogIndex, long BatchIndex);