using Recyclarr.ServarrApi.MediaManagement;

namespace Recyclarr.Cli.Pipelines.MediaManagement.PipelinePhases;

internal class MediaManagementApiFetchPhase(IMediaManagementApiService api)
    : IPipelinePhase<MediaManagementPipelineContext>
{
    public async Task<PipelineFlow> Execute(
        MediaManagementPipelineContext context,
        CancellationToken ct
    )
    {
        context.ApiFetchOutput = await api.GetMediaManagement(ct);
        return PipelineFlow.Continue;
    }
}
