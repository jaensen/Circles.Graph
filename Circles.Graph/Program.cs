using System.Diagnostics;
using Circles.Graph.EventSourcing.Balances;
using Circles.Graph.EventSourcing.Trust;
using Circles.Graph.Pathfinder;
using Circles.Graph.Pathfinder.DTOs;

namespace Circles.Graph;

public static class Program
{
    private const string HttpRpcEndpoint = "https://rpc.aboutcircles.com/";
    private const string WsRpcEndpoint = "wss://rpc.aboutcircles.com/ws";

    public static async Task Main()
    {
        CancellationTokenSource cts = new();

        // var eventSource = new EventSource(HttpRpcEndpoint, WsRpcEndpoint);

        var trustGraphAggregator = new TrustGraphAggregator();
        var balanceGraphAggregator = new BalanceGraphAggregator();

        Console.WriteLine("Initializing synthetic accounts...");
        var scenario = new RandomScenario(10_000);

        Console.WriteLine("Processing trust and balance events...");
        var l = 0L;
        // Initialize synthetic accounts
        foreach (var evt in scenario.GetTrustEvents(50_000))
        {
            l++;
            trustGraphAggregator.ProcessEvent(evt);
            if (l % 100000 == 0)
            {
                Console.WriteLine($"Processed {l} trust events");
            }
        }

        l = 0;
        foreach (var evt in scenario.GetTransferEvents(10_000))
        {
            l++;
            balanceGraphAggregator.ProcessEvent(evt);
            if (l % 100000 == 0)
            {
                Console.WriteLine($"Processed {l} balance events");
            }
        }

        // Sample 100 addresses from the trust graph
        Console.WriteLine("Sampling 100 addresses...");
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

        var balanceGraph = balanceGraphAggregator.GetState();
        var graphFactory = new GraphFactory();

        Console.WriteLine("Creating capacity graph...");
        Stopwatch stopwatch = new();
        stopwatch.Restart();
        var capacityGraph = graphFactory.CreateCapacityGraph(balanceGraph, trustGraph);
        stopwatch.Stop();
        Console.WriteLine($"Created capacity graph in {stopwatch.ElapsedMilliseconds}ms");

        var pathfinder = new V2Pathfinder(trustGraph, capacityGraph, new GraphFactory());
        var maxFlowResponses = new List<MaxFlowResponse>();

        for (int i = 0; i < sampledAddresses.Count; i++)
        {
            for (int j = 0; j < sampledAddresses.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                Console.WriteLine($"Computing max flow between {sampledAddresses[i]} and {sampledAddresses[j]}");
                stopwatch.Restart();

                try
                {
                    var response = pathfinder.ComputeMaxFlow(new FlowRequest
                    {
                        Source = sampledAddresses[i],
                        Sink = sampledAddresses[j],
                        TargetFlow = "100"
                    }).Result;
                    Console.WriteLine($"Max flow: {response.MaxFlow}, Time: {stopwatch.ElapsedMilliseconds}ms");

                    maxFlowResponses.Add(response);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue; 
                }

                stopwatch.Stop();
            }
        }
    }
}