using Circles.Common;

namespace Circles.Graph.EventSourcing;

public interface IAggregator<in TEvent, out TState>
    where TEvent : IIndexEvent
{
    TState GetState();
    TState ProcessEvent(TEvent indexEvent);
    TState RevertToBlock(long blockNumber, long timestamp);
}