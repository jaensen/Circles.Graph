using System.Numerics;
using Circles.Graph.Graphs;
using Google.OrTools.Graph;

namespace Circles.Graph.Pathfinder
{
    public static class GraphExtensions
    {
        /// <summary>
        /// Computes the maximum flow from source to sink in the FlowGraph using Google's OR-Tools.
        /// Scales capacities to fit within Int64 range and scales flows back after computation.
        /// Only scales if values exceed the Int64 range.
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

            // Add a super-source node
            int superSourceIndex = nodeIndex++;

            // Find the maximum capacity
            BigInteger maxCapacity = graph.Edges.Max(e => e.CurrentCapacity);
            if (targetFlow > maxCapacity)
            {
                maxCapacity = targetFlow;
            }

            // Determine the number of digits to truncate
            int maxCapacityDigits = maxCapacity.ToString().Length;
            int maxInt64Digits = long.MaxValue.ToString().Length;
            int digitsToRemove = maxCapacityDigits - maxInt64Digits;
            if (digitsToRemove < 0) digitsToRemove = 0;

            // Primary scaling factor
            BigInteger scalingFactor = BigInteger.Pow(10, digitsToRemove);

            // Secondary scaling if necessary
            while (targetFlow / scalingFactor > long.MaxValue)
            {
                scalingFactor *= 10;
                digitsToRemove++;
            }

            // Method to adjust capacities by truncating least significant digits
            BigInteger AdjustCapacity(BigInteger capacity)
            {
                return capacity / scalingFactor;
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

                // Adjust the capacity
                BigInteger adjustedCapacity = AdjustCapacity(edge.CurrentCapacity);

                // Ensure capacity fits within Int64
                if (adjustedCapacity > long.MaxValue)
                {
                    throw new Exception("Adjusted capacity exceeds Int64 maximum value even after scaling.");
                }

                long capacity = (long)adjustedCapacity;

                // Add the arc
                int arc = maxFlow.AddArcWithCapacity(from, to, capacity);

                // Store the mapping
                edgeToArc[edge] = arc;
            }

            // Adjust the target flow
            BigInteger adjustedTargetFlow = AdjustCapacity(targetFlow);

            // Ensure target flow fits within Int64
            if (adjustedTargetFlow > long.MaxValue)
            {
                throw new Exception("Adjusted target flow exceeds Int64 maximum value even after secondary scaling.");
            }

            long adjustedTargetFlowLong = (long)adjustedTargetFlow;

            // Add an arc from the super-source to the original source
            int originalSourceIndex = nodeIndices[source];
            maxFlow.AddArcWithCapacity(superSourceIndex, originalSourceIndex, adjustedTargetFlowLong);

            // Set the source and sink indices
            int sourceIndex = superSourceIndex; // Use the super-source as the new source
            int sinkIndex = nodeIndices[sink];

            // Solve the max flow problem
            MaxFlow.Status status = maxFlow.Solve(sourceIndex, sinkIndex);
            if (status != MaxFlow.Status.OPTIMAL)
            {
                throw new Exception("Max flow could not find an optimal solution.");
            }

            // Get the maximum flow and scale it back
            long maxFlowValue = maxFlow.OptimalFlow();
            BigInteger resultFlow = maxFlowValue * scalingFactor;

            Console.WriteLine($"Max flow: {resultFlow}; Scaling factor: {scalingFactor}.");

            // Update the flows in the edges
            foreach (var edge in graph.Edges)
            {
                int arc = edgeToArc[edge];
                long flow = maxFlow.Flow(arc);

                // Scale the flow back
                BigInteger scaledFlow = flow * scalingFactor;
                edge.Flow = scaledFlow;
                edge.CurrentCapacity -= scaledFlow;

                // Update reverse edge capacities if necessary
                if (edge.ReverseEdge != null)
                {
                    edge.ReverseEdge.CurrentCapacity += scaledFlow;
                }
            }

            // Return the accumulated flow
            return resultFlow;
        }
    }
}