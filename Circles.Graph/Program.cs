using System.Diagnostics;
using Circles.Common;
using Circles.Graph.Data;
using Circles.Graph.Events;
using Circles.Graph.EventSourcing.Balances;
using Circles.Graph.EventSourcing.Trust;
using Circles.Graph.Pathfinder;
using Circles.Graph.Pathfinder.DTOs;
using Circles.Graph.Rpc;

namespace Circles.Graph;

public static class Program
{
    private const string HttpRpcEndpoint = "https://rpc.aboutcircles.com/";
    private const string WsRpcEndpoint = "wss://rpc.aboutcircles.com/ws";

    public static async Task Main()
    {
        CancellationTokenSource cts = new();

        var eventSource = new EventSource(HttpRpcEndpoint, WsRpcEndpoint);

        var trustGraphAggregator = new TrustGraphAggregator();
        var balanceGraphAggregator = new BalanceGraphAggregator();

        var scenario = new RandomScenario(100_000);

        _ = Task.Run(async () =>
        {
            // Initialize synthetic accounts
            foreach (var evt in scenario.GetTrustEvents(1_000_000))
            {
                trustGraphAggregator.ProcessEvent(evt);
            }

            foreach (var evt in scenario.GetTransferEvents(1_000_000))
            {
                balanceGraphAggregator.ProcessEvent(evt);
            }
        }, cts.Token);

        // Check if the edge counts are increasing. If not, break the loop.
        var lastTrustEdgeCount = 0;
        var lastBalanceEdgeCount = 0;

        while (!cts.IsCancellationRequested)
        {
            await Task.Delay(1000, cts.Token);

            var trustEdgeCount = trustGraphAggregator.GetState().Edges.Count;
            var trustNodeCount = trustGraphAggregator.GetState().Nodes.Count;
            var balanceEdgeCount = balanceGraphAggregator.GetState().Edges.Count;
            var balanceNodeCount = balanceGraphAggregator.GetState().Nodes.Count;
            Console.WriteLine(
                $"Trust Graph: {trustNodeCount} nodes, {trustEdgeCount} edges. Balance Graph: {balanceNodeCount} nodes, {balanceEdgeCount} edges.");

            if (trustEdgeCount == lastTrustEdgeCount && balanceEdgeCount == lastBalanceEdgeCount)
            {
                break;
            }

            lastTrustEdgeCount = trustEdgeCount;
            lastBalanceEdgeCount = balanceEdgeCount;
        }

        // Sample 100 addresses from the trust graph
        var sampledAddresses = new List<string>();
        var trustGraph = trustGraphAggregator.GetState();
        var trustGraphNodes = trustGraph.Nodes.Values.ToArray();
        var random = new Random();
        for (int i = 0; i < 100; i++)
        {
            var node = trustGraphNodes[random.Next(trustGraphNodes.Length)];
            sampledAddresses.Add(node.Address);
            // Console.WriteLine($"Node: {node.Address}");
        }

        // Split the addresses into 2 groups. One is the sender and the other the recipient. 
        // Then go through the pairs and calculate the max flow between them.
        var pathfinder = new V2Pathfinder(trustGraph, balanceGraphAggregator.GetState(), new GraphFactory());
        var maxFlowResponses = new List<MaxFlowResponse>();

        Stopwatch stopwatch = new();

        for (int i = 0; i < sampledAddresses.Count; i++)
        {
            for (int j = 0; j < sampledAddresses.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                Console.WriteLine($"Computing max flow between {sampledAddresses[i]} and {sampledAddresses[j]}");
                stopwatch.Start();

                var response = pathfinder.ComputeMaxFlow(new FlowRequest
                {
                    Source = sampledAddresses[i],
                    Sink = sampledAddresses[j],
                    TargetFlow = "100000000000000000000"
                }).Result;

                stopwatch.Stop();
                Console.WriteLine($"Max flow: {response.MaxFlow}, Time: {stopwatch.ElapsedMilliseconds}ms");

                maxFlowResponses.Add(response);
            }
        }
    }
}