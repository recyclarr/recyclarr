using Recyclarr.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat.Cache;
using Recyclarr.Common.Extensions;
using Recyclarr.ServarrApi.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

internal class CustomFormatApiFetchPhase(
    ICustomFormatApiService api,
    ICachePersister<CustomFormatCache> cachePersister
) : IPipelinePhase<CustomFormatPipelineContext>
{
    public async Task<PipelineFlow> Execute(
        CustomFormatPipelineContext context,
        CancellationToken ct
    )
    {
        context.Cache = cachePersister.Load();
        var result = await api.GetCustomFormats(ct);
        context.ApiFetchOutput.AddRange(result);
        return PipelineFlow.Continue;
    }
}
