using System.Numerics;
using System.Collections.Immutable;

namespace Circles.Graph.Graphs;

public record FlowGraph(
    ImmutableDictionary<string, Node> Nodes,
    ImmutableDictionary<string, AvatarNode> AvatarNodes,
    ImmutableDictionary<string, BalanceNode> BalanceNodes,
    ImmutableHashSet<FlowEdge> Edges)
    : IGraph<FlowEdge>
{
    public FlowGraph()
        : this(
            ImmutableDictionary<string, Node>.Empty,
            ImmutableDictionary<string, AvatarNode>.Empty,
            ImmutableDictionary<string, BalanceNode>.Empty,
            ImmutableHashSet<FlowEdge>.Empty)
    {
    }

    public FlowGraph AddAvatar(string avatarAddress)
    {
        if (!AvatarNodes.ContainsKey(avatarAddress))
        {
            var avatarNode = new AvatarNode(avatarAddress);
            var newAvatarNodes = AvatarNodes.SetItem(avatarAddress, avatarNode);
            var newNodes = Nodes.SetItem(avatarAddress, avatarNode);

            return this with { AvatarNodes = newAvatarNodes, Nodes = newNodes };
        }

        return this;
    }

    public FlowGraph AddBalanceNode(string address, string token, BigInteger amount)
    {
        var balanceNode = new BalanceNode(address, token, amount, 0); // Assuming LastChangeTimestamp is 0
        var newBalanceNodes = BalanceNodes.SetItem(balanceNode.Address, balanceNode);
        var newNodes = Nodes.SetItem(balanceNode.Address, balanceNode);

        return this with { BalanceNodes = newBalanceNodes, Nodes = newNodes };
    }

    public FlowGraph AddCapacity(CapacityGraph capacityGraph)
    {
        var graph = this;
        foreach (var capacityEdge in capacityGraph.Edges)
        {
            graph = graph.AddCapacityEdge(capacityGraph, capacityEdge);
        }

        return graph;
    }

    public FlowGraph AddCapacityEdge(CapacityGraph capacityGraph, CapacityEdge capacityEdge)
    {
        var from = capacityEdge.From;
        var to = capacityEdge.To;
        var token = capacityEdge.Token;
        var capacity = capacityEdge.InitialCapacity;

        // Create edge and reverse edge
        var edge = new FlowEdge(from, to, token, capacity);
        var reverseEdge = new FlowEdge(to, from, token, 0);

        // Note: Setting ReverseEdge in immutable records can lead to circular references, which is problematic.
        // In this adapted code, we're not setting the ReverseEdge property to maintain immutability.
        // If necessary, you can manage reverse edges using a separate mapping or by computing them when needed.

        var newEdges = Edges.Add(edge).Add(reverseEdge);

        var graph = this;

        // Create nodes if they don't exist
        if (!graph.Nodes.ContainsKey(from))
        {
            var capacityFromNode = capacityGraph.Nodes[from];
            if (capacityFromNode is AvatarNode)
            {
                graph = graph.AddAvatar(capacityFromNode.Address);
            }
            else if (capacityFromNode is BalanceNode fromBalance)
            {
                graph = graph.AddBalanceNode(fromBalance.Address, fromBalance.Token, fromBalance.Amount);
            }
        }

        if (!graph.Nodes.ContainsKey(to))
        {
            var capacityToNode = capacityGraph.Nodes[to];
            if (capacityToNode is AvatarNode)
            {
                graph = graph.AddAvatar(capacityToNode.Address);
            }
            else if (capacityToNode is BalanceNode toBalance)
            {
                graph = graph.AddBalanceNode(toBalance.Address, toBalance.Token, toBalance.Amount);
            }
        }

        // Now get the nodes
        var fromNode = graph.Nodes[from];
        var toNode = graph.Nodes[to];

        // Update adjacency lists
        var updatedFromNode = fromNode with
        {
            OutEdges = fromNode.OutEdges.Add(edge),
            InEdges = fromNode.InEdges.Add(reverseEdge)
        };

        var updatedToNode = toNode with
        {
            InEdges = toNode.InEdges.Add(edge),
            OutEdges = toNode.OutEdges.Add(reverseEdge)
        };

        // Update Nodes
        var newNodes = graph.Nodes
            .SetItem(from, updatedFromNode)
            .SetItem(to, updatedToNode);

        // Update AvatarNodes and BalanceNodes as needed
        if (updatedFromNode is AvatarNode updatedFromAvatarNode)
        {
            var newAvatarNodes = graph.AvatarNodes.SetItem(from, updatedFromAvatarNode);
            graph = graph with { AvatarNodes = newAvatarNodes };
        }
        else if (updatedFromNode is BalanceNode updatedFromBalanceNode)
        {
            var newBalanceNodes = graph.BalanceNodes.SetItem(from, updatedFromBalanceNode);
            graph = graph with { BalanceNodes = newBalanceNodes };
        }

        if (updatedToNode is AvatarNode updatedToAvatarNode)
        {
            var newAvatarNodes = graph.AvatarNodes.SetItem(to, updatedToAvatarNode);
            graph = graph with { AvatarNodes = newAvatarNodes };
        }
        else if (updatedToNode is BalanceNode updatedToBalanceNode)
        {
            var newBalanceNodes = graph.BalanceNodes.SetItem(to, updatedToBalanceNode);
            graph = graph with { BalanceNodes = newBalanceNodes };
        }

        // Update the graph
        graph = graph with { Nodes = newNodes, Edges = newEdges };

        return graph;
    }

    public FlowGraph AddFlowEdge(FlowGraph flowGraph, FlowEdge flowEdge)
    {
        var graph = this;

        var fromNode = flowGraph.Nodes[flowEdge.From];
        if (!graph.Nodes.ContainsKey(fromNode.Address))
        {
            if (fromNode is AvatarNode)
            {
                graph = graph.AddAvatar(fromNode.Address);
            }
            else if (fromNode is BalanceNode fromBalance)
            {
                graph = graph.AddBalanceNode(fromBalance.Address, fromBalance.Token, fromBalance.Amount);
            }
        }

        var toNode = flowGraph.Nodes[flowEdge.To];
        if (!graph.Nodes.ContainsKey(toNode.Address))
        {
            if (toNode is AvatarNode)
            {
                graph = graph.AddAvatar(toNode.Address);
            }
            else if (toNode is BalanceNode toBalance)
            {
                graph = graph.AddBalanceNode(toBalance.Address, toBalance.Token, toBalance.Amount);
            }
        }

        // Get updated nodes
        var from = graph.Nodes[fromNode.Address];
        var to = graph.Nodes[toNode.Address];

        // Check if edge already exists
        if (from.OutEdges.OfType<FlowEdge>().Any(o => o.To == to.Address && o.Flow == flowEdge.Flow))
        {
            return graph;
        }

        if (to.InEdges.OfType<FlowEdge>().Any(o => o.From == from.Address && o.Flow == flowEdge.Flow))
        {
            return graph;
        }

        // Create new edges
        var newFlowEdge = new FlowEdge(from.Address, to.Address, flowEdge.Token, flowEdge.CurrentCapacity)
        {
            Flow = flowEdge.Flow
            // ReverseEdge is not set in this immutable version
        };

        var newReverseEdge = new FlowEdge(to.Address, from.Address, flowEdge.Token,
            flowEdge.ReverseEdge?.CurrentCapacity ?? BigInteger.Zero)
        {
            Flow = flowEdge.ReverseEdge?.Flow ?? BigInteger.Zero
            // ReverseEdge is not set in this immutable version
        };

        var newEdges = graph.Edges.Add(newFlowEdge).Add(newReverseEdge);

        // Update adjacency lists
        var updatedFromNode = @from with
        {
            OutEdges = from.OutEdges.Add(newFlowEdge),
            InEdges = from.InEdges.Add(newReverseEdge)
        };

        var updatedToNode = to with
        {
            InEdges = to.InEdges.Add(newFlowEdge),
            OutEdges = to.OutEdges.Add(newReverseEdge)
        };

        // Update Nodes
        var newNodes = graph.Nodes
            .SetItem(from.Address, updatedFromNode)
            .SetItem(to.Address, updatedToNode);

        // Update AvatarNodes and BalanceNodes as needed
        if (updatedFromNode is AvatarNode updatedFromAvatarNode)
        {
            var newAvatarNodes = graph.AvatarNodes.SetItem(from.Address, updatedFromAvatarNode);
            graph = graph with { AvatarNodes = newAvatarNodes };
        }
        else if (updatedFromNode is BalanceNode updatedFromBalanceNode)
        {
            var newBalanceNodes = graph.BalanceNodes.SetItem(from.Address, updatedFromBalanceNode);
            graph = graph with { BalanceNodes = newBalanceNodes };
        }

        if (updatedToNode is AvatarNode updatedToAvatarNode)
        {
            var newAvatarNodes = graph.AvatarNodes.SetItem(to.Address, updatedToAvatarNode);
            graph = graph with { AvatarNodes = newAvatarNodes };
        }
        else if (updatedToNode is BalanceNode updatedToBalanceNode)
        {
            var newBalanceNodes = graph.BalanceNodes.SetItem(to.Address, updatedToBalanceNode);
            graph = graph with { BalanceNodes = newBalanceNodes };
        }

        // Update the graph
        graph = graph with { Nodes = newNodes, Edges = newEdges };

        return graph;
    }

    /// <summary>
    /// Searches the graph for liquid paths from the source node to the sink node.
    /// </summary>
    /// <param name="sourceNode">The source</param>
    /// <param name="sinkNode">The sink</param>
    /// <param name="threshold">Only consider edges with more or equal flow</param>
    /// <returns>A list of paths with flow</returns>
    public List<List<FlowEdge>> ExtractPathsWithFlow(string sourceNode, string sinkNode, BigInteger threshold)
    {
        var resultPaths = new List<List<FlowEdge>>();
        var visited = new HashSet<string>();

        // A helper method to perform DFS and collect paths with positive flow
        void Dfs(string currentNode, List<FlowEdge> currentPath)
        {
            if (currentNode == sinkNode)
            {
                resultPaths.Add(new List<FlowEdge>(currentPath)); // Store a copy of the path
                return;
            }

            if (!Nodes.TryGetValue(currentNode, out var node)) return;

            visited.Add(currentNode);

            foreach (var edge in node.OutEdges.OfType<FlowEdge>())
            {
                if (edge.Flow >= threshold && !visited.Contains(edge.To))
                {
                    currentPath.Add(edge); // Add edge to the current path
                    Dfs(edge.To, currentPath); // Recursively go deeper
                    currentPath.RemoveAt(currentPath.Count - 1); // Backtrack
                }
            }

            visited.Remove(currentNode);
        }

        // Start DFS from the source node
        Dfs(sourceNode, new List<FlowEdge>());

        return resultPaths;
    }
}