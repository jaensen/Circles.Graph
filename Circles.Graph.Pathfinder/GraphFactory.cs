using System.Diagnostics;
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
        var capacityGraph = new CapacityGraph();

        var stopWatch = new Stopwatch();
        stopWatch.Start();

        // Step 1: Create unified list of nodes
        var allAvatarAddresses = new HashSet<string>(balanceGraph.AvatarNodes.Values.Select(a => a.Address));
        foreach (var avatar in trustGraph.AvatarNodes.Values)
        {
            allAvatarAddresses.Add(avatar.Address);
        }

        stopWatch.Stop();
        Console.WriteLine($"Created unified list of nodes in {stopWatch.ElapsedMilliseconds}ms");

        stopWatch.Restart();
        foreach (var address in allAvatarAddresses)
        {
            capacityGraph = capacityGraph.AddAvatar(address);
        }

        stopWatch.Stop();
        Console.WriteLine($"Added avatars in {stopWatch.ElapsedMilliseconds}ms");

        stopWatch.Restart();

        foreach (var balanceNode in balanceGraph.BalanceNodes.Values)
        {
            capacityGraph = capacityGraph.AddBalanceNode(balanceNode.Address, balanceNode.Token, balanceNode.Amount);
        }

        stopWatch.Stop();
        Console.WriteLine($"Added balance nodes in {stopWatch.ElapsedMilliseconds}ms");

        stopWatch.Restart();

        // Step 2: Copy capacity edges from the balance graph
        foreach (var capacityEdge in balanceGraph.Edges)
        {
            capacityGraph = capacityGraph.AddCapacityEdge(
                capacityEdge.From,
                capacityEdge.To,
                capacityEdge.Token,
                capacityEdge.InitialCapacity
            );
        }

        stopWatch.Stop();
        Console.WriteLine($"Added balance edges in {stopWatch.ElapsedMilliseconds}ms");

        stopWatch.Restart();

        // Step 3: Create additional capacity edges from the trust graph
        // Precompute a tokenIssuer-to-acceptingNodes dictionary
        var tokenTrustMap = trustGraph.Edges
            .GroupBy(e => e.To)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.From).ToHashSet()
            );

        stopWatch.Stop();
        Console.WriteLine($"Precomputed token trust map in {stopWatch.ElapsedMilliseconds}ms");

        stopWatch.Restart();

        Console.WriteLine($"Looping through {balanceGraph.BalanceNodes.Count} balance nodes");
        foreach (var balanceNode in balanceGraph.BalanceNodes.Values)
        {
            if (!tokenTrustMap.TryGetValue(balanceNode.Token, out var acceptingNodes))
            {
                continue;
            }

            // Console.WriteLine($"{acceptingNodes.Count} accepting nodes for token {balanceNode.Token} balance of node {balanceNode.Address}");
            foreach (var acceptingNode in acceptingNodes)
            {
                // Avoid self-loops
                if (acceptingNode != balanceNode.HolderAddress)
                {
                    capacityGraph = capacityGraph.AddCapacityEdge(
                        balanceNode.Address,
                        acceptingNode,
                        balanceNode.Token,
                        balanceNode.Amount
                    );
                }
            }
        }

        stopWatch.Stop();
        Console.WriteLine($"Added trust edges in {stopWatch.ElapsedMilliseconds}ms");

        return capacityGraph;
    }

    public FlowGraph CreateFlowGraph(CapacityGraph capacityGraph)
    {
        var flowGraph = new FlowGraph();
        flowGraph = flowGraph.AddCapacity(capacityGraph);
        return flowGraph;
    }
}