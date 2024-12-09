using System.Numerics;
using Circles.Graph.Graphs;

namespace Circles.Graph.EventSourcing.Trust;

public class AddTrustAction(string from, string to, BigInteger expiryTime) : IEventAction<TrustGraph>
{
    readonly TrustEdge _edge = new(from, to, expiryTime);

    public TrustGraph Apply(TrustGraph state)
    {
        var graph = state;

        if (!graph.Nodes.TryGetValue(from, out var fromNode))
        {
            (graph, fromNode) = graph.AddAvatar(from);
        }

        if (!graph.Nodes.TryGetValue(to, out var toNode))
        {
            (graph, toNode) = graph.AddAvatar(to);
        }

        if (graph.Edges.Contains(_edge))
        {
            return graph;
            // throw new InvalidOperationException(
            //     $"AddTrustAction: Edge {from} -> {to} (expiry: {expiryTime}) already exists.");
        }

        var newEdges = graph.Edges.Add(_edge);

        var updatedFromNode = fromNode with { OutEdges = fromNode.OutEdges.Add(_edge) };
        var updatedToNode = toNode with { InEdges = toNode.InEdges.Add(_edge) };

        var newNodes = graph.Nodes
            .SetItem(from, updatedFromNode)
            .SetItem(to, updatedToNode);

        var newAvatarNodes = graph.AvatarNodes
            .SetItem(from, (AvatarNode)updatedFromNode)
            .SetItem(to, (AvatarNode)updatedToNode);

        graph = new TrustGraph(newNodes, newAvatarNodes, newEdges);

        return graph;
    }

    public IEventAction<TrustGraph> GetInverseAction()
    {
        return new RemoveTrustAction(from, to, expiryTime);
    }
}