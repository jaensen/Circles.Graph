using System.Numerics;

namespace Circles.Common;

public interface IIndexEvent
{
    long BlockNumber { get; }
    long Timestamp { get; }
    int TransactionIndex { get; }
    int LogIndex { get; }
}

public record TransferEvent(
    long BlockNumber,
    long Timestamp,
    int TransactionIndex,
    int LogIndex,
    int BatchIndex,
    string From,
    string To,
    string TokenAddress,
    BigInteger Value) : IIndexEvent;

public record TrustEvent(
    long BlockNumber,
    long Timestamp,
    int TransactionIndex,
    int LogIndex,
    int BatchIndex,
    string Truster,
    string Trustee,
    BigInteger ExpiryTime) : IIndexEvent;