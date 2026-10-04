using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Servarr.MediaNaming;

namespace Recyclarr.Pipelines.Plan;

/// <summary>
/// Planned media naming for one instance. Sonarr and Radarr naming share no fields, so each
/// service's subtype carries its own data (ADR-023).
/// </summary>
internal abstract class PlannedMediaNaming;

internal sealed class PlannedSonarrMediaNaming : PlannedMediaNaming
{
    public required SonarrNamingData Data { get; init; }
    public IReadOnlyList<SonarrNamingReferenceMismatchOutcome> Mismatches { get; init; } = [];
}

internal sealed class PlannedRadarrMediaNaming : PlannedMediaNaming
{
    public required RadarrNamingData Data { get; init; }
    public IReadOnlyList<RadarrNamingReferenceMismatchOutcome> Mismatches { get; init; } = [];
}
