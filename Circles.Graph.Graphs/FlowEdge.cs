using System.Numerics;

namespace Circles.Graph.Graphs;

/// <summary>
/// Represents a flow edge for actual token transfers between nodes.
/// </summary>
public record FlowEdge(string From, string To, string Token, BigInteger InitialCapacity)
    : CapacityEdge(From, To, Token, InitialCapacity)
{
    public BigInteger CurrentCapacity { get; set; } = InitialCapacity;
    public BigInteger Flow { get; set; }
    public FlowEdge? ReverseEdge { get; init; }
}