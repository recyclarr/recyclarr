using Recyclarr.Cli.Pipelines.CustomFormat.State;
using Recyclarr.Common.Extensions;
using Recyclarr.ServarrApi.CustomFormat;
using Recyclarr.SyncState;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

internal class CustomFormatApiFetchPhase(
    ICustomFormatApiService api,
    ISyncStatePersister<CustomFormatMappings> statePersister
) : IPipelinePhase<CustomFormatPipelineContext>
{
    public async Task<PipelineFlow> Execute(
        CustomFormatPipelineContext context,
        CancellationToken ct
    )
    {
        context.State = statePersister.Load();
        var result = await api.GetCustomFormats(ct);
        context.ApiFetchOutput.AddRange(result);
        return PipelineFlow.Continue;
    }
}
