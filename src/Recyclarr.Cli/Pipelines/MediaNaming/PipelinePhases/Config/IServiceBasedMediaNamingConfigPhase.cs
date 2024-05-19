using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.TrashGuide.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;

public interface IServiceBasedMediaNamingConfigPhase
{
    Task<MediaNamingDto> ProcessNaming(IMediaNamingGuideService guide, NamingFormatLookup lookup);
}
