using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.QualityProfile;

namespace Recyclarr.Pipelines.QualityProfile;

/// <summary>
/// Sonarr quality profiles have no fields beyond the shared ones.
/// </summary>
internal class SonarrQualityProfileFields : IQualityProfileServiceFields
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
        QualityProfileData desired
    ) => shared;
}
