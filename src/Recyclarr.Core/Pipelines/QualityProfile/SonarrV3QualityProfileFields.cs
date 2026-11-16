using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.QualityProfile;

namespace Recyclarr.Pipelines.QualityProfile;

/// <summary>
/// Sonarr v4 profiles (served through the v3 API) have no fields beyond the shared ones.
/// </summary>
internal class SonarrV3QualityProfileFields : IQualityProfileServiceFields
{
    public Task LoadAsync(CancellationToken ct) => Task.CompletedTask;

    public QualityProfileData ApplyDesired(
        QualityProfileData profile,
        PlannedQualityProfile planned
    ) => profile;

    public bool HasChanges(QualityProfileData current, QualityProfileData desired) => false;

    public PlannedQualityProfileResource Describe(
        PlannedQualityProfileResource shared,
        QualityProfileData? current,
        QualityProfileData desired,
        PlannedQualityProfile planned
    ) => new SonarrPlannedQualityProfileResource(shared, []);
}
