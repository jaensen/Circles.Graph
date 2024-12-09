using System.Runtime.CompilerServices;
using Circles.Graph.Events;
using Circles.Graph.Rpc;

namespace Circles.Graph.Data;

public class EventSource(string httpUrl, string wsUrl, int keyRetentionCapacity = 10000)
{
    private readonly FixedSizeHashSet<EventKey> _yieldedEventKeys = new(keyRetentionCapacity);
    private LiveEventSubscriber? _liveEventSubscriber;
    private HistoricalEventLoader? _historicalEventLoader;

    public async IAsyncEnumerable<CirclesEventResult> GetEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Initialize live event subscriber
        _liveEventSubscriber = new LiveEventSubscriber(wsUrl);
        await _liveEventSubscriber.InitializeAsync();

        // Wait for the live subscription to be ready
        await _liveEventSubscriber.SubscriptionReady;

        // Load historical events
        _historicalEventLoader = new HistoricalEventLoader(httpUrl);
        var historicalEvents = await _historicalEventLoader.LoadHistoricalEventsAsync();

        // Yield historical events in order
        foreach (var eventResult in historicalEvents)
        {
            var eventKey = GetEventKey(eventResult);
            if (_yieldedEventKeys.Add(eventKey))
            {
                yield return eventResult;
            }
        }

        // Process live events indefinitely
        await foreach (var liveEvent in _liveEventSubscriber.GetLiveEventsAsync(cancellationToken))
        {
            var eventKey = GetEventKey(liveEvent);
            if (_yieldedEventKeys.Add(eventKey))
            {
                yield return liveEvent;
            }
        }
    }

    private static EventKey GetEventKey(CirclesEventResult eventResult)
    {
        var values = eventResult.Values;

        if (values == null)
            throw new InvalidOperationException("Event values cannot be null.");

        long blockNumber = values.BlockNumberLong;
        long timestamp = values.TimestampLong;
        long transactionIndex = values.TransactionIndexInt;
        long logIndex = values.LogIndexInt;

        long batchIndex = 0;
        if (values is TransferEventValues transferValues)
        {
            batchIndex = transferValues.BatchIndexLong;
        }

        return new EventKey(blockNumber, timestamp, transactionIndex, logIndex, batchIndex);
    }
}