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

internal class CustomFormatSyncOperation(
    ILogger log,
    ICustomFormatService api,
    ICustomFormatStatePersister statePersister,
    CustomFormatTransactionLogger cfLogger,
    IServiceConfiguration config
) : SyncOperation<CustomFormatComputeResult>
{
    public override PipelineType Type => PipelineType.CustomFormat;
    public override string Description => "Custom Format";

    protected override async Task<CustomFormatComputeResult> Compute(
        PipelinePlan plan,
        IPipelinePublisher publisher,
        CancellationToken ct
    )
    {
        // Fetch phase
        var state = statePersister.Load();
        var apiFetchOutput = await api.GetCustomFormats(ct);

        // Transaction phase
        var plannedCfs = plan.CustomFormats;
        var transactions = new CustomFormatTransactionData();

        // Build lookups for O(1) access
        var serviceCfsById = apiFetchOutput.ToDictionary(cf => cf.Id);
        var serviceCfsByName = apiFetchOutput.ToLookup(
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

            var storedId = state.FindId(guideCf.MappingKey);

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
        var deletionCandidates = state
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

        var validServiceIds = apiFetchOutput.Select(cf => cf.Id).ToList();
        return new CustomFormatComputeResult(transactions, validServiceIds, state);
    }

    protected override async Task Persist(
        CustomFormatComputeResult computeResult,
        IPipelinePublisher publisher,
        CancellationToken ct
    )
    {
        cfLogger.LogTransactions(computeResult.Transactions, publisher);

        var transactions = computeResult.Transactions;

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

        computeResult.State.Update(computeResult);
        statePersister.Save(computeResult.State);
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
