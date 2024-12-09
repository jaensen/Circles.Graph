using System.Numerics;

namespace Circles.Graph.Graphs;

/// <summary>
/// Represents a trust relationship between two nodes.
/// </summary>
public record TrustEdge(string From, string To, BigInteger ExpiryTime) : Edge(From, To);