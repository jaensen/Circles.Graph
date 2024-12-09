using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Circles.Graph.Rpc;

namespace Circles.Graph;

public class LiveEventSubscriber(string wsUrl)
{
    private readonly Channel<CirclesEventResult> _eventChannel = Channel.CreateUnbounded<CirclesEventResult>();
    private readonly TaskCompletionSource _subscriptionReady = new();
    private ClientWebSocket? _clientWebSocket;

    public Task SubscriptionReady => _subscriptionReady.Task;

    public async Task InitializeAsync()
    {
        _clientWebSocket = new ClientWebSocket();
        await _clientWebSocket.ConnectAsync(new Uri(wsUrl), CancellationToken.None);

        var subscriptionRequest = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "eth_subscribe",
            @params = new object[] { "circles", new { } }
        };

        await SendWebSocketMessageAsync(subscriptionRequest);
        Console.WriteLine("Subscribed to live events");

        // Signal that the subscription is ready
        _subscriptionReady.SetResult();

        // Start receiving events
        _ = Task.Run(ReceiveEventsAsync);
    }

    private async Task ReceiveEventsAsync()
    {
        var buffer = new byte[8192];
        var messageBuilder = new StringBuilder();

        while (_clientWebSocket?.State == WebSocketState.Open)
        {
            var result = await ReceiveWebSocketMessageAsync(buffer, messageBuilder);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                break;
            }

            var evt = JsonSerializer.Deserialize<CirclesEvent>(messageBuilder.ToString(),
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new CirclesEventResultConverter() }
                });

            messageBuilder.Clear();

            if (evt?.Params?.Result == null) continue;

            foreach (var eventResult in evt.Params.Result)
            {
                // Write the event to the channel
                await _eventChannel.Writer.WriteAsync(eventResult);
            }
        }

        // Indicate that no more events will be written
        _eventChannel.Writer.Complete();
    }

    public async IAsyncEnumerable<CirclesEventResult> GetLiveEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (await _eventChannel.Reader.WaitToReadAsync(cancellationToken))
        {
            while (_eventChannel.Reader.TryRead(out var liveEvent))
            {
                yield return liveEvent;
            }
        }
    }

    private async Task SendWebSocketMessageAsync(object message)
    {
        var jsonMessage = JsonSerializer.Serialize(message);
        var messageBytes = Encoding.UTF8.GetBytes(jsonMessage);
        await _clientWebSocket!.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true,
            CancellationToken.None);
    }

    private async Task<WebSocketReceiveResult> ReceiveWebSocketMessageAsync(byte[] buffer, StringBuilder messageBuilder)
    {
        WebSocketReceiveResult result;
        do
        {
            result = await _clientWebSocket!.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
        } while (!result.EndOfMessage);

        return result;
    }
}