using System.Numerics;
using Circles.Graph.Graphs;

namespace Circles.Graph.EventSourcing.Balances;

public class SubtractFromBalance(string accountAddress, string tokenAddress, BigInteger value, long timestamp)
    : IEventAction<BalanceGraph>
{
    public BalanceGraph Apply(BalanceGraph state)
    {
        var currentBalance = state.GetBalance(accountAddress, tokenAddress);
        if (currentBalance - value < 0)
        {
            throw new InvalidOperationException("Balance cannot be negative.");
        }
        var newState = state.SetBalance(accountAddress, tokenAddress, currentBalance - value, timestamp);

        return newState;
    }

    public IEventAction<BalanceGraph> GetInverseAction()
    {
        return new AddToBalance(accountAddress, tokenAddress, value, timestamp);
    }
}