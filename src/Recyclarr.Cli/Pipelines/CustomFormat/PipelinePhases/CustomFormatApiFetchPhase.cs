using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.CustomFormat;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

public class CustomFormatApiFetchPhase(ICustomFormatApiService api)
{
    public async Task<IReadOnlyCollection<CustomFormatData>> Execute(IServiceConfiguration config)
    {
        var result = await api.GetCustomFormats(config);
        return result.AsReadOnly();
    }
}
