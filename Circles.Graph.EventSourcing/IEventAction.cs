namespace Circles.Graph.EventSourcing;

public interface IEventAction<TState>
{
    TState Apply(TState state);
    IEventAction<TState> GetInverseAction();
}