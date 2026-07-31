using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.MediaNaming.Sonarr;

internal record SonarrNamingComputeResult(
    SonarrNamingData? Current,
    SonarrNamingData? Desired,
    SonarrNamingPipelineResult Result
) : IPipelineResultSource
{
    PipelineResult IPipelineResultSource.Result => Result;
}
