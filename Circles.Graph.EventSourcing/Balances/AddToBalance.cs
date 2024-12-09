using System.Numerics;
using Circles.Graph.Graphs;

namespace Circles.Graph.EventSourcing.Balances;

public class AddToBalance(string accountAddress, string tokenAddress, BigInteger value, long timestamp)
    : IEventAction<BalanceGraph>
{
    public BalanceGraph Apply(BalanceGraph state)
    {
        var currentBalance = state.GetBalance(accountAddress, tokenAddress);
        var newState = state.SetBalance(accountAddress, tokenAddress, currentBalance + value, timestamp);

        return newState;
    }

    public IEventAction<BalanceGraph> GetInverseAction()
    {
        return new SubtractFromBalance(accountAddress, tokenAddress, value, timestamp);
    }
}