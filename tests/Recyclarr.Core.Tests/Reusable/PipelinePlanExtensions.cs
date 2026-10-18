using Recyclarr.Pipelines.Plan;
using Recyclarr.Sync;

namespace Recyclarr.Core.Tests.Reusable;

/// <summary>
/// Reads each shipped pipeline's typed plan from a built <see cref="PipelinePlan"/>, so planning
/// tests assert on results without repeating the plan type lookups.
/// </summary>
internal static class PipelinePlanExtensions
{
    extension(PipelinePlan plan)
    {
        public IReadOnlyCollection<PlannedCustomFormat> CustomFormats =>
            plan.Get<PlannedCustomFormats>().Value?.Items ?? [];

        public IReadOnlyCollection<PlannedQualityProfile> QualityProfiles =>
            plan.Get<PlannedQualityProfiles>().Value?.Items ?? [];

        public bool QualitySizesAvailable => plan.Get<PlannedQualitySizes>().Value is not null;
        public bool MediaNamingAvailable => plan.Get<PlannedMediaNaming>().Value is not null;

        public bool MediaManagementAvailable =>
            plan.Get<PlannedMediaManagement>().Value is not null;

        public PlannedQualitySizes QualitySizes => Required(plan.Get<PlannedQualitySizes>());
        public PlannedMediaNaming MediaNaming => Required(plan.Get<PlannedMediaNaming>());

        public PlannedMediaManagement MediaManagement =>
            Required(plan.Get<PlannedMediaManagement>());

        /// <summary>Outcomes from every pipeline, in the order the pipelines were planned.</summary>
        public IReadOnlyList<SyncOutcome> Outcomes =>
            [
                .. plan.Get<PlannedCustomFormats>().Outcomes,
                .. plan.Get<PlannedQualityProfiles>().Outcomes,
                .. plan.Get<PlannedQualitySizes>().Outcomes,
                .. plan.Get<PlannedMediaNaming>().Outcomes,
                .. plan.Get<PlannedMediaManagement>().Outcomes,
            ];

        public bool HasErrors => plan.Outcomes.Any(x => x.Level == SyncDiagnosticLevel.Error);
    }

    private static T Required<T>(PlannedPipeline<T> planned)
        where T : class =>
        planned.Value ?? throw new InvalidOperationException($"{typeof(T).Name} was not planned");
}
