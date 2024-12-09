using Circles.Graph.EventSourcing.Balances;
using Circles.Graph.EventSourcing.Trust;

namespace Circles.Graph.EventSourcing;

public sealed class Aggregates
{
    public TrustGraphAggregator TrustGraph { get; } = new();
    public BalanceGraphAggregator BalanceGraph { get; } = new();
}