using Recyclarr.Servarr.MediaNaming;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.MediaNaming.Radarr;

internal record RadarrNamingComputeResult(
    RadarrNamingData? Current,
    RadarrNamingData? Desired,
    RadarrNamingPipelineResult Result
) : IPipelineResultSource
{
    PipelineResult IPipelineResultSource.Result => Result;
}
