using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.Plan;

internal class PlannedSonarrMediaNaming
{
    public required SonarrMediaNamingDto Dto { get; init; }
}

internal class PlannedRadarrMediaNaming
{
    public required RadarrMediaNamingDto Dto { get; init; }
}
