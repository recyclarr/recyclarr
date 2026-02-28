using Recyclarr.Cli.Pipelines.CustomFormat.State;
using Recyclarr.Common.Extensions;
using Recyclarr.Servarr.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

internal class CustomFormatApiFetchPhase(
    ICustomFormatService api,
    ICustomFormatStatePersister statePersister
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
