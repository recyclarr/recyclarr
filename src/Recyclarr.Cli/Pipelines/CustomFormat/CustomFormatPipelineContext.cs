using Recyclarr.Cli.Pipelines.CustomFormat.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Sync;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

internal class CustomFormatPipelineContext : PipelineContext
{
    public override string PipelineDescription => "Custom Format";
    public override PipelineType PipelineType => PipelineType.CustomFormat;

    public IList<CustomFormatResource> ApiFetchOutput { get; init; } = [];
    public CustomFormatTransactionData TransactionOutput { get; set; } = null!;
    public CustomFormatCache Cache { get; set; } = null!;
}
