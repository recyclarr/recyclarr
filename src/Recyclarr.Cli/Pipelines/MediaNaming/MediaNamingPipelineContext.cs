using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;
using Recyclarr.Common;
using Recyclarr.ServarrApi.MediaNaming;

namespace Recyclarr.Cli.Pipelines.MediaNaming;

public class MediaNamingPipelineContext : IPipelineContext
{
    public string PipelineDescription => "Media Naming Pipeline";
    public IReadOnlyCollection<SupportedServices> SupportedServiceTypes { get; } = new[]
    {
        SupportedServices.Sonarr,
        SupportedServices.Radarr
    };

    public ProcessedNamingConfig ConfigOutput { get; set; } = default!;
    public MediaNamingDto ApiFetchOutput { get; set; } = default!;
    public MediaNamingDto TransactionOutput { get; set; } = default!;
}
