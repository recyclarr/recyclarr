using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Common.Extensions;
using Recyclarr.ServarrApi.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

public class CustomFormatApiFetchPhase(ICustomFormatApiService api)
    : IApiFetchPipelinePhase<CustomFormatPipelineContext>
{
    public async Task Execute(CustomFormatPipelineContext context)
    {
        var result = await api.GetCustomFormats();
        context.ApiFetchOutput.AddRange(result);
        context.Cache.RemoveStale(result);
    }
}
