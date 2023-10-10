using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.TrashGuide.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;

public abstract class ServiceBasedMediaNamingConfigPhase<TConfig> : IServiceBasedMediaNamingConfigPhase
    where TConfig : IServiceConfiguration
{
    public Task<MediaNamingDto> ProcessNaming(
        IServiceConfiguration config,
        IMediaNamingGuideService guide,
        NamingFormatLookup lookup)
    {
        return ProcessNaming((TConfig) config, guide, lookup);
    }

    protected abstract Task<MediaNamingDto> ProcessNaming(
        TConfig config,
        IMediaNamingGuideService guide,
        NamingFormatLookup lookup);
}
