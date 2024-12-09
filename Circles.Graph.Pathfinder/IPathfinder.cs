using Circles.Graph.Pathfinder.DTOs;

namespace Circles.Graph.Pathfinder;

public interface IPathfinder
{
    public Task<MaxFlowResponse> ComputeMaxFlow(FlowRequest request);
}