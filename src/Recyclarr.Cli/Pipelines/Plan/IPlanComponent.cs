namespace Recyclarr.Cli.Pipelines.Plan;

internal interface IPlanComponent
{
    void Process(PipelinePlan plan, PlanDiagnostics diagnostics);
}
