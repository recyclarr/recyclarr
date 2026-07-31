namespace Recyclarr.Pipelines.Plan;

internal interface IPlanComponent
{
    void Process(PipelinePlan plan);
}
