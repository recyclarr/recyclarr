using Recyclarr.Pipelines.Plan;
using Recyclarr.Servarr.QualityProfile;

namespace Recyclarr.Pipelines.QualityProfile;

/// <summary>
/// Quality profile behavior for fields that only one service has (ADR-023). The sync operation
/// handles the fields both services share; DI selects the implementation for the detected service
/// API target (Sonarr v3, Sonarr v5, Radarr). An implementation is scoped to one sync and may keep
/// data from
/// <see cref="LoadAsync"/> for the later calls.
/// </summary>
internal interface IQualityProfileServiceFields
{
    /// <summary>
    /// Fetches the service data that the other members need. Called once before any profile is
    /// processed.
    /// </summary>
    Task LoadAsync(CancellationToken ct);

    /// <summary>
    /// Returns the profile with service-owned fields set to their desired values.
    /// </summary>
    QualityProfileData ApplyDesired(QualityProfileData profile, PlannedQualityProfile planned);

    bool HasChanges(QualityProfileData current, QualityProfileData desired);

    /// <summary>
    /// Adds the service-owned values to the shared values reported for a profile. A null
    /// <paramref name="current"/> means the sync creates the profile.
    /// </summary>
    PlannedQualityProfileResource Describe(
        PlannedQualityProfileResource shared,
        QualityProfileData? current,
        QualityProfileData desired,
        PlannedQualityProfile planned
    );
}
