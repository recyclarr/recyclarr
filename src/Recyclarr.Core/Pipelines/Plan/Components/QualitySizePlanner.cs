using Recyclarr.Config.Models;

namespace Recyclarr.Pipelines.Plan.Components;

/// <summary>
/// Plans instance-wide quality sizes. Whether the service stores sizes globally or per quality
/// profile decides the planning, so all of it lives in the
/// <see cref="IInstanceQualitySizePlanner"/> that DI selects (ADR-023).
/// </summary>
internal class QualitySizePlanner(IInstanceQualitySizePlanner planner)
    : IPipelinePlanner<PlannedQualitySizes>
{
    public PlannedQualitySizes? Plan(PlanningContext context) => planner.Plan(context);
}

/// <summary>
/// Builds one service version's instance-wide quality sizes. Returns null when the instance has
/// none to sync.
/// </summary>
internal interface IInstanceQualitySizePlanner
{
    PlannedQualitySizes? Plan(PlanningContext context);
}

/// <summary>
/// Used where quality sizes are global (Sonarr v4, Radarr): resolves the instance
/// <c>quality_definition</c>.
/// </summary>
internal class GlobalInstanceQualitySizePlanner(
    QualitySizeResolver resolver,
    IServiceConfiguration config,
    ILogger log
) : IInstanceQualitySizePlanner
{
    public PlannedQualitySizes? Plan(PlanningContext context)
    {
        var configSizeData = config.QualityDefinition;
        if (configSizeData is null)
        {
            log.Debug("No quality_definition configured, skipping quality sizes");
            return null;
        }

        return resolver.Resolve(configSizeData, context.Add);
    }
}

/// <summary>
/// Used where quality sizes are per quality profile (Sonarr v5): plans no instance-wide sizes. The
/// quality profile planner assigns sizes to each profile instead (PDR-008).
/// </summary>
internal class PerProfileInstanceQualitySizePlanner : IInstanceQualitySizePlanner
{
    public PlannedQualitySizes? Plan(PlanningContext context) => null;
}
