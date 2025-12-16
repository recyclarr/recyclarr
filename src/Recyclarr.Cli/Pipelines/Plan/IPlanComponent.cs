using Recyclarr.Sync.Events;

namespace Recyclarr.Cli.Pipelines.Plan;

internal interface IPlanComponent
{
    void Process(PipelinePlan plan, ISyncEventPublisher events);
}
