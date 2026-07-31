using Recyclarr.Servarr.MediaManagement;
using Recyclarr.Sync.Results;

namespace Recyclarr.Pipelines.MediaManagement;

internal record MediaManagementComputeResult(
    MediaManagementData Current,
    MediaManagementData Desired,
    MediaManagementPipelineResult Result
) : IPipelineResultSource
{
    PipelineResult IPipelineResultSource.Result => Result;
}
