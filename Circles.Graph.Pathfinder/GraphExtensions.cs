using System.Numerics;
using Circles.Graph.Graphs;
using Google.OrTools.Graph;

namespace Circles.Graph.Pathfinder;

public static class GraphExtensions
{
    /// <summary>
    /// Computes the maximum flow from source to sink in the FlowGraph using Google's OR-Tools.
    /// This version does not perform any scaling on capacities.
    /// </summary>
    /// <param name="graph">The flow graph instance.</param>
    /// <param name="source">The source node identifier.</param>
    /// <param name="sink">The sink node identifier.</param>
    /// <param name="targetFlow">The desired flow value to reach.</param>
    /// <returns>The total flow value up to the target flow.</returns>
    public static BigInteger ComputeMaxFlowWithPaths(
        this FlowGraph graph,
        string source,
        string sink,
        BigInteger targetFlow)
    {
        // Map node addresses to indices
        var nodeIndices = new Dictionary<string, int>();
        int nodeIndex = 0;
        foreach (var node in graph.Nodes.Values)
        {
            nodeIndices[node.Address] = nodeIndex++;
        }

        // Ensure capacities fit within Int64
        if (graph.Edges.Any(edge => edge.CurrentCapacity > long.MaxValue))
        {
            throw new Exception("Edge capacities exceed the maximum supported value of Int64.");
        }

        if (targetFlow > long.MaxValue)
        {
            throw new Exception("Target flow exceeds the maximum supported value of Int64.");
        }

        // Create the MaxFlow solver
        var maxFlow = new MaxFlow();

        // Map edges to arc indices
        var edgeToArc = new Dictionary<FlowEdge, int>();

        // Add arcs (edges) to the solver
        foreach (var edge in graph.Edges)
        {
            int from = nodeIndices[edge.From];
            int to = nodeIndices[edge.To];

            // Use the capacity directly
            long capacity = (long)edge.CurrentCapacity;

            // Add the arc
            int arc = maxFlow.AddArcWithCapacity(from, to, capacity);

            // Store the mapping
            edgeToArc[edge] = arc;
        }

        // Set the source and sink indices
        int sourceIndex = nodeIndices[source]; // Use the super-source as the new source
        int sinkIndex = nodeIndices[sink];

        // Solve the max flow problem
        MaxFlow.Status status = maxFlow.Solve(sourceIndex, sinkIndex);
        if (status != MaxFlow.Status.OPTIMAL)
        {
            throw new Exception("Max flow could not find an optimal solution:" + status.ToString());
        }

        // Get the maximum flow
        long maxFlowValue = maxFlow.OptimalFlow();
        BigInteger resultFlow = maxFlowValue;

        Console.WriteLine($"Max flow: {resultFlow}");

        // Update the flows in the edges
        foreach (var edge in graph.Edges)
        {
            int arc = edgeToArc[edge];
            long flow = maxFlow.Flow(arc);

            // Update the flow and capacities directly
            edge.Flow = flow;
            edge.CurrentCapacity -= flow;

            // Update reverse edge capacities if necessary
            if (edge.ReverseEdge != null)
            {
                edge.ReverseEdge.CurrentCapacity += flow;
            }
        }

        // Return the accumulated flow
        return resultFlow;
    }
}