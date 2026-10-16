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

    public IReadOnlyList<QualityProfileUpdateComponent> FindChanges(
        QualityProfileData current,
        QualityProfileData desired
    ) => [];

    public QualityProfileControlledState DescribeCreate(
        QualityProfileControlledState shared,
        QualityProfileData desired,
        PlannedQualityProfile planned
    ) => new SonarrQualityProfileControlledState(shared, []);
}
