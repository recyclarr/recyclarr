using Autofac.Features.Indexed;
using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases.Config;
using Recyclarr.Config.Models;
using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;

public record InvalidNamingConfig(string Type, string ConfigValue);

public record ProcessedNamingConfig
{
    public required MediaNamingDto Dto { get; init; }
    public IReadOnlyCollection<InvalidNamingConfig> InvalidNaming { get; init; } = [];
}

public class MediaNamingConfigPhase(
    IMediaNamingGuideService guide,
    IIndex<SupportedServices, IServiceBasedMediaNamingConfigPhase> configPhaseStrategyFactory,
    IServiceConfiguration config
) : IConfigPipelinePhase<MediaNamingPipelineContext>
{
    public async Task Execute(MediaNamingPipelineContext context, CancellationToken ct)
    {
        var lookup = new NamingFormatLookup();
        var strategy = configPhaseStrategyFactory[config.ServiceType];
        var dto = await strategy.ProcessNaming(guide, lookup);

        context.ConfigOutput = new ProcessedNamingConfig
        {
            Dto = dto,
            InvalidNaming = lookup.Errors,
        };
    }
}
