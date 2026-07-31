using Recyclarr.Pipelines.MediaNaming.Radarr;
using Recyclarr.Pipelines.MediaNaming.Sonarr;
using Recyclarr.Servarr.MediaNaming;

namespace Recyclarr.Pipelines.Plan;

internal class PlannedSonarrMediaNaming
{
    public required SonarrNamingData Data { get; init; }
    public IReadOnlyList<SonarrNamingReferenceMismatchOutcome> Mismatches { get; init; } = [];
}

internal class PlannedRadarrMediaNaming
{
    public required RadarrNamingData Data { get; init; }
    public IReadOnlyList<RadarrNamingReferenceMismatchOutcome> Mismatches { get; init; } = [];
}
