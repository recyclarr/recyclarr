using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;

internal interface IServiceBasedMediaNamingConfigPhase
{
    Task<MediaNamingDto> ProcessNaming(MediaNamingResourceQuery guide, NamingFormatLookup lookup);
}
