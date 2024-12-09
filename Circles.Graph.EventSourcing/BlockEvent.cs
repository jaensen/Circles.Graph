using Circles.Common;

namespace Circles.Graph.EventSourcing;

public class BlockEvent(long blockNumber, long timestamp) : IIndexEvent
{
    public long BlockNumber => blockNumber;
    public long Timestamp => timestamp;
    public int TransactionIndex => -1;
    public int LogIndex => -1;
}