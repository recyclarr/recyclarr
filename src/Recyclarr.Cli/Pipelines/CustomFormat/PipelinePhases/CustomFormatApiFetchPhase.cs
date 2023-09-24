using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.CustomFormat;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

public class CustomFormatApiFetchPhase
{
    private readonly ICustomFormatApiService _api;

    public CustomFormatApiFetchPhase(ICustomFormatApiService api)
    {
        _api = api;
    }

    public async Task<IReadOnlyCollection<CustomFormatData>> Execute(IServiceConfiguration config)
    {
        var result = await _api.GetCustomFormats(config);
        return result.AsReadOnly();
    }
}
