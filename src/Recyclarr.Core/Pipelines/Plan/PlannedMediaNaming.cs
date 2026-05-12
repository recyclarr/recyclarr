using Recyclarr.Servarr.MediaNaming;

namespace Recyclarr.Pipelines.Plan;

internal class PlannedSonarrMediaNaming
{
    public required SonarrNamingData Data { get; init; }
}

internal class PlannedRadarrMediaNaming
{
    public required RadarrNamingData Data { get; init; }
}
