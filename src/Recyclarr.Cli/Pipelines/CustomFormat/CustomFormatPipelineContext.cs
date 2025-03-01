using Recyclarr.Cli.Pipelines.CustomFormat.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

internal class CustomFormatPipelineContext : PipelineContext
{
    public override string PipelineDescription => "Custom Format";

    public IList<CustomFormatData> ConfigOutput { get; init; } = [];
    public IList<CustomFormatData> ApiFetchOutput { get; init; } = [];
    public CustomFormatTransactionData TransactionOutput { get; set; } = null!;
    public IReadOnlyCollection<string> InvalidFormats { get; set; } = null!;
    public CustomFormatCache Cache { get; set; } = null!;
}
