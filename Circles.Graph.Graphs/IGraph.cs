using System.Collections.Immutable;

namespace Circles.Graph.Graphs;

public interface IGraph<TEdge>
    where TEdge : Edge
{
    ImmutableDictionary<string, Node> Nodes { get; }
    ImmutableHashSet<TEdge> Edges { get; }
}