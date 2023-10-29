using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

public class CustomFormatApiFetchPhase(ICustomFormatApiService api)
    : IApiFetchPipelinePhase<CustomFormatPipelineContext>
{
    public async Task Execute(CustomFormatPipelineContext context, IServiceConfiguration config)
    {
        var result = await api.GetCustomFormats(config);
        context.ApiFetchOutput.AddRange(result);
        context.Cache.RemoveStale(result);
    }
}
