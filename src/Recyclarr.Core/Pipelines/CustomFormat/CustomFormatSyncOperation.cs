using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines.CustomFormat.Models;
using Recyclarr.Pipelines.CustomFormat.State;
using Recyclarr.Pipelines.Plan;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.CustomFormat;
using Recyclarr.Sync;
using Recyclarr.SyncState;

namespace Recyclarr.Pipelines.CustomFormat;

internal record CustomFormatPreviewData(
    CustomFormatTransactionData Transactions,
    PipelinePlan Plan
);

internal class CustomFormatSyncOperation(
    ILogger log,
    ICustomFormatService api,
    ICustomFormatStatePersister statePersister,
    CustomFormatTransactionLogger cfLogger,
    IServiceConfiguration config,
    IEnumerable<IPreviewRenderer<CustomFormatPreviewData>> previewRenderers
) : ISyncOperation, ISyncStateSource
{
    private readonly List<CustomFormatResource> _apiFetchOutput = [];
    private CustomFormatTransactionData _transactionOutput = null!;
    private TrashIdMappingStore _state = null!;
    private PipelinePlan _plan = null!;

    public PipelineType Type => PipelineType.CustomFormat;
    public string Description => "Custom Format";
    public IReadOnlyList<PipelineType> Dependencies => [];

    public bool ShouldSkip(PipelinePlan plan) => false;

    // ISyncStateSource implementation
    public IEnumerable<TrashIdMapping> SyncedMappings =>
        _transactionOutput
            .NewCustomFormats.Concat(_transactionOutput.UpdatedCustomFormats)
            .Concat(_transactionOutput.UnchangedCustomFormats)
            .Select(cf => new TrashIdMapping(cf.TrashId, cf.Name, cf.Id));

    public IEnumerable<int> DeletedIds =>
        _transactionOutput.DeletedCustomFormats.Select(m => m.ServiceId);

    public IEnumerable<int> ValidServiceIds => _apiFetchOutput.Select(cf => cf.Id);

    public async Task Compute(PipelinePlan plan, IPipelinePublisher publisher, CancellationToken ct)
    {
        _plan = plan;

        // Fetch phase
        _state = statePersister.Load();
        var result = await api.GetCustomFormats(ct);
        _apiFetchOutput.AddRange(result);

        // Transaction phase
        var plannedCfs = plan.CustomFormats;
        var transactions = new CustomFormatTransactionData();

        // Build lookups for O(1) access
        var serviceCfsById = _apiFetchOutput.ToDictionary(cf => cf.Id);
        var serviceCfsByName = _apiFetchOutput.ToLookup(
            cf => cf.Name,
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var planned in plannedCfs)
        {
            var guideCf = planned.Resource;
            log.Debug(
                "Process transaction for guide CF {TrashId} ({Name})",
                guideCf.TrashId,
                guideCf.Name
            );

            var storedId = _state.FindId(guideCf.MappingKey);

            if (storedId.HasValue)
            {
                ProcessCachedCf(
                    guideCf,
                    storedId.Value,
                    serviceCfsById,
                    serviceCfsByName,
                    transactions
                );
            }
            else
            {
                ProcessUncachedCf(guideCf, serviceCfsByName, transactions);
            }
        }

        // Always identify deletion candidates (regardless of delete toggle - checked in persistence)
        var deletionCandidates = _state
            .Mappings
            // Custom format must be in the state but NOT in the user's config
            .Where(map => plannedCfs.All(cf => cf.Resource.TrashId != map.TrashId))
            // Also, that state-only CF must exist in the service (otherwise there is nothing to delete)
            .Where(map => serviceCfsById.ContainsKey(map.ServiceId))
            .ToList();

        // Build set of service IDs that are being actively managed (updated or unchanged)
        var managedServiceIds = transactions
            .UpdatedCustomFormats.Concat(transactions.UnchangedCustomFormats)
            .Select(cf => cf.Id)
            .ToHashSet();

        // Separate valid deletions from invalid state entries (duplicate service IDs)
        foreach (var candidate in deletionCandidates)
        {
            if (managedServiceIds.Contains(candidate.ServiceId))
            {
                // State inconsistency: service ID is claimed by both a managed CF and an orphan
                transactions.InvalidCacheEntries.Add(candidate);
            }
            else
            {
                transactions.DeletedCustomFormats.Add(candidate);
            }
        }

        _transactionOutput = transactions;
    }

    public async Task Persist(IPipelinePublisher publisher, CancellationToken ct)
    {
        cfLogger.LogTransactions(_transactionOutput, publisher);

        var transactions = _transactionOutput;

        foreach (var cf in transactions.NewCustomFormats)
        {
            var response = await api.CreateCustomFormat(cf, ct);
            if (response is not null)
            {
                cf.Id = response.Id;
            }
        }

        foreach (var dto in transactions.UpdatedCustomFormats)
        {
            await api.UpdateCustomFormat(dto, ct);
        }

        if (config.DeleteOldCustomFormats)
        {
            foreach (var map in transactions.DeletedCustomFormats)
            {
                await api.DeleteCustomFormat(map.ServiceId, ct);
            }
        }

        _state.Update(this);
        statePersister.Save(_state);
    }

    public void RenderPreview(string instanceName)
    {
        var renderer = previewRenderers.FirstOrDefault();
        renderer?.Render(
            Description,
            instanceName,
            new CustomFormatPreviewData(_transactionOutput, _plan)
        );
    }

    private void ProcessCachedCf(
        CustomFormatResource guideCf,
        int storedId,
        Dictionary<int, CustomFormatResource> serviceCfsById,
        ILookup<string, CustomFormatResource> serviceCfsByName,
        CustomFormatTransactionData transactions
    )
    {
        if (serviceCfsById.TryGetValue(storedId, out var serviceCf))
        {
            // ID-first: Found by stored ID - update regardless of name
            guideCf.Id = storedId;

            if (!serviceCf.Name.EqualsIgnoreCase(guideCf.Name))
            {
                log.Debug(
                    "CF {TrashId} will be renamed from '{ServiceName}' to '{GuideName}'",
                    guideCf.TrashId,
                    serviceCf.Name,
                    guideCf.Name
                );
            }

            AddUpdatedOrUnchanged(guideCf, serviceCf, transactions);
        }
        else
        {
            // Stale state: stored ID no longer exists in service
            log.Debug(
                "Stored service ID {StoredId} for CF {TrashId} no longer exists in service",
                storedId,
                guideCf.TrashId
            );

            // Check for name collision before creating
            ProcessNameCollision(guideCf, serviceCfsByName, transactions);
        }
    }

    private static void ProcessUncachedCf(
        CustomFormatResource guideCf,
        ILookup<string, CustomFormatResource> serviceCfsByName,
        CustomFormatTransactionData transactions
    )
    {
        ProcessNameCollision(guideCf, serviceCfsByName, transactions);
    }

    private static void ProcessNameCollision(
        CustomFormatResource guideCf,
        ILookup<string, CustomFormatResource> serviceCfsByName,
        CustomFormatTransactionData transactions
    )
    {
        var nameMatches = serviceCfsByName[guideCf.Name].ToList();

        switch (nameMatches.Count)
        {
            case 0:
                // No collision - safe to create
                transactions.NewCustomFormats.Add(guideCf);
                break;

            case 1:
                // Config is authoritative: adopt the existing service CF
                guideCf.Id = nameMatches[0].Id;
                transactions.ReplacedCustomFormats.Add(guideCf.Name);
                AddUpdatedOrUnchanged(guideCf, nameMatches[0], transactions);
                break;

            default:
                // Multiple matches - ambiguous
                transactions.AmbiguousCustomFormats.Add(
                    new AmbiguousMatch(
                        guideCf.Name,
                        nameMatches.Select(cf => (cf.Name, cf.Id)).ToList()
                    )
                );
                break;
        }
    }

    private static void AddUpdatedOrUnchanged(
        CustomFormatResource guideCf,
        CustomFormatResource serviceCf,
        CustomFormatTransactionData transactions
    )
    {
        if (!IsEquivalent(guideCf, serviceCf))
        {
            transactions.UpdatedCustomFormats.Add(guideCf);
        }
        else
        {
            transactions.UnchangedCustomFormats.Add(guideCf);
        }
    }

    // Compares custom format data for equivalence, ignoring record type differences.
    // Guide CFs are SonarrCustomFormatResource/RadarrCustomFormatResource (derived types),
    // while API responses deserialize to base CustomFormatResource.
    private static bool IsEquivalent(CustomFormatResource a, CustomFormatResource b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        // FullOuterHashJoin matches specs by name. For matched pairs, delegates to
        // CustomFormatSpecificationData equality. Returns false for unmatched specs.
        var specsEqual = a
            .Specifications.FullOuterHashJoin(
                b.Specifications,
                x => x.Name,
                x => x.Name,
                _ => false,
                _ => false,
                (x, y) => x == y
            )
            .All(x => x);

        return a.Id == b.Id
            && a.Name == b.Name
            && a.IncludeCustomFormatWhenRenaming == b.IncludeCustomFormatWhenRenaming
            && specsEqual;
    }
}
