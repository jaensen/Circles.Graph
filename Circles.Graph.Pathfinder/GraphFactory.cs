using System.Numerics;
using Circles.Graph.Graphs;

namespace Circles.Graph.Pathfinder;

public class GraphFactory
{
    /// <summary>
    /// Takes a balance graph and a trust graph and creates a capacity graph from them.
    /// </summary>
    /// <param name="balanceGraph">The balance graph to use.</param>
    /// <param name="trustGraph">The trust graph to use.</param>
    /// <returns>A capacity graph created from the balance and trust graphs.</returns>
    public CapacityGraph CreateCapacityGraph(BalanceGraph balanceGraph, TrustGraph trustGraph)
    {
        // Take the balance and trust graphs and create a capacity graph.
        // 1. Create a unified list of nodes from both graphs
        // 2. Leave the capacity edges from the balance graph in place
        // 3. Create more capacity edges based on the trust graph:
        //    - For each balance, check if there is a node that is willing to accept the balance (is trusting the token issuer)
        //    - If there is, create a capacity edge from the balance node to the accepting node

        var capacityGraph = new CapacityGraph();

        // Step 1: Create a unified list of nodes from both graphs
        foreach (var avatar in balanceGraph.AvatarNodes.Values)
        {
            capacityGraph.AddAvatar(avatar.Address);
        }

        foreach (var avatar in trustGraph.AvatarNodes.Values)
        {
            capacityGraph.AddAvatar(avatar.Address);
        }

        // Add BalanceNodes
        foreach (var balanceNode in balanceGraph.BalanceNodes.Values)
        {
            capacityGraph.AddBalanceNode(balanceNode.Address, balanceNode.Token, balanceNode.Amount);
        }

        // Step 2: Leave the capacity edges from the balance graph in place
        foreach (var capacityEdge in balanceGraph.Edges)
        {
            capacityGraph.AddCapacityEdge(
                capacityEdge.From,
                capacityEdge.To,
                capacityEdge.Token,
                capacityEdge.InitialCapacity
            );
        }

        // Step 3: Create more capacity edges based on the trust graph
        // Optimization: Precompute a trustee-to-trusters lookup dictionary
        var trusteeToTrusters = new Dictionary<string, List<string>>();

        foreach (var edge in trustGraph.Edges)
        {
            if (!trusteeToTrusters.TryGetValue(edge.To, out var trusters))
            {
                trusters = new List<string>();
                trusteeToTrusters[edge.To] = trusters;
            }

            trusters.Add(edge.From);
        }

        foreach (var balanceNode in balanceGraph.BalanceNodes.Values)
        {
            string tokenIssuer = balanceNode.Token;

            if (trusteeToTrusters.TryGetValue(tokenIssuer, out var acceptingNodes))
            {
                foreach (var acceptingNode in acceptingNodes)
                {
                    // Avoid creating edges to self or invalid nodes
                    if (acceptingNode == balanceNode.HolderAddress)
                        continue;

                    capacityGraph.AddCapacityEdge(
                        balanceNode.Address,
                        acceptingNode,
                        balanceNode.Token,
                        balanceNode.Amount
                    );
                }
            }
        }

        return capacityGraph;
    }

    public FlowGraph CreateFlowGraph(CapacityGraph capacityGraph)
    {
        var flowGraph = new FlowGraph();
        flowGraph.AddCapacity(capacityGraph);

        return flowGraph;
    }
}