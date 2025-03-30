using Recyclarr.Common.Extensions;
using Recyclarr.ServarrApi.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

internal class CustomFormatApiFetchPhase(ICustomFormatApiService api)
    : IPipelinePhase<CustomFormatPipelineContext>
{
    public async Task<PipelineFlow> Execute(
        CustomFormatPipelineContext context,
        CancellationToken ct
    )
    {
        var result = await api.GetCustomFormats(ct);
        context.ApiFetchOutput.AddRange(result);
        context.Cache.RemoveStale(result);
        return PipelineFlow.Continue;
    }
}
