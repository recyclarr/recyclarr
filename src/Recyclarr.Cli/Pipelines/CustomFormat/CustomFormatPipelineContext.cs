using Recyclarr.Cli.Pipelines.CustomFormat.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Cli.Pipelines.Generic;
using Recyclarr.TrashGuide;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

public class CustomFormatPipelineContext : IPipelineContext
{
    public string PipelineDescription => "Custom Format";
    public IReadOnlyCollection<SupportedServices> SupportedServiceTypes { get; } =
        [SupportedServices.Sonarr, SupportedServices.Radarr];

    public IList<CustomFormatData> ConfigOutput { get; init; } = [];
    public IList<CustomFormatData> ApiFetchOutput { get; init; } = [];
    public CustomFormatTransactionData TransactionOutput { get; set; } = default!;
    public IReadOnlyCollection<string> InvalidFormats { get; set; } = default!;
    public CustomFormatCache Cache { get; set; } = default!;
}
