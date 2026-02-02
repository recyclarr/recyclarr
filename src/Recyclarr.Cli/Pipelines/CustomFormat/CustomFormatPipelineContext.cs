using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Cli.Pipelines.CustomFormat.State;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Sync;
using Recyclarr.SyncState;

namespace Recyclarr.Cli.Pipelines.CustomFormat;

internal class CustomFormatPipelineContext : PipelineContext, ISyncStateSource, IPipelineMetadata
{
    public static PipelineType PipelineType => PipelineType.CustomFormat;
    public static IReadOnlyList<PipelineType> Dependencies => [];

    public override string PipelineDescription => "Custom Format";

    public IList<CustomFormatResource> ApiFetchOutput { get; init; } = [];
    public CustomFormatTransactionData TransactionOutput { get; set; } = null!;
    public TrashIdMappingStore<CustomFormatMappings> State { get; set; } = null!;

    // ISyncStateSource implementation
    public IEnumerable<TrashIdMapping> SyncedMappings =>
        TransactionOutput
            .NewCustomFormats.Concat(TransactionOutput.UpdatedCustomFormats)
            .Concat(TransactionOutput.UnchangedCustomFormats)
            .Select(cf => new TrashIdMapping(cf.TrashId, cf.Name, cf.Id));

    public IEnumerable<int> DeletedIds =>
        TransactionOutput.DeletedCustomFormats.Select(m => m.ServiceId);

    public IEnumerable<int> ValidServiceIds => ApiFetchOutput.Select(cf => cf.Id);
}
