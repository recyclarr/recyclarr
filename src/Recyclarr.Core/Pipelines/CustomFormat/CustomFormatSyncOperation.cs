using System.Net;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.Pipelines.CustomFormat.Models;
using Recyclarr.Pipelines.CustomFormat.State;
using Recyclarr.Pipelines.Plan;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.Servarr.CustomFormat;
using Recyclarr.Sync;
using Recyclarr.Sync.Results;
using Recyclarr.SyncState;
using Refit;

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
        var outcomes = MapPlanOutcomes(plan).ToList();
        var deltas = new List<CustomFormatDelta>();
        var incompleteResources = plan.Outcomes.OfType<InvalidCustomFormatTrashIdOutcome>().Count();

        var sourceInfo = plannedCfs.ToDictionary(
            cf => cf.Resource.TrashId,
            CreateSourceInfo,
            StringComparer.OrdinalIgnoreCase
        );

        // Build lookups for O(1) access
        var serviceCfsById = apiFetchOutput.ToDictionary(cf => cf.Id);
        var serviceCfsByName = apiFetchOutput.ToLookup(
            cf => cf.Name,
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var planned in plannedCfs)
        {
            var guideCf = planned.Resource;
            var provenance = sourceInfo[guideCf.TrashId];
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
                    transactions,
                    provenance,
                    outcomes,
                    deltas
                );
            }
            else
            {
                ProcessUncachedCf(
                    guideCf,
                    serviceCfsByName,
                    transactions,
                    provenance,
                    outcomes,
                    deltas
                );
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
                incompleteResources++;

                var managed = transactions
                    .UpdatedCustomFormats.Concat(transactions.UnchangedCustomFormats)
                    .First(cf => cf.Id == candidate.ServiceId);
                outcomes.Add(
                    new CustomFormatStateConflictOutcome(
                        ToIdentity(candidate),
                        ToIdentity(managed),
                        candidate.ServiceId
                    )
                );
            }
            else if (config.DeleteOldCustomFormats)
            {
                transactions.DeletedCustomFormats.Add(candidate);
                deltas.Add(new CustomFormatDeleteDelta(ToIdentity(candidate)));
            }
        }

        incompleteResources += transactions.AmbiguousCustomFormats.Count;
        var completedResources =
            transactions.NewCustomFormats.Count
            + transactions.UpdatedCustomFormats.Count
            + transactions.UnchangedCustomFormats.Count
            + transactions.DeletedCustomFormats.Count;
        var result = new CustomFormatPipelineResult(
            completedResources,
            incompleteResources,
            outcomes,
            deltas
        );

        var validServiceIds = apiFetchOutput.Select(cf => cf.Id).ToList();
        var computeResult = new CustomFormatComputeResult(
            transactions,
            validServiceIds,
            state,
            sourceInfo,
            result,
            incompleteResources
        );
        cfLogger.LogTransactions(transactions, publisher, result);
        return computeResult;
    }

    protected override async Task Persist(
        CustomFormatComputeResult computeResult,
        IPipelinePublisher publisher,
        CancellationToken ct
    )
    {
        var transactions = computeResult.Transactions;
        var completedResources = transactions.UnchangedCustomFormats.Count;
        var incompleteResources = computeResult.TransactionIncompleteResources;
        var persistenceOutcomes = new List<CustomFormatOutcome>();

        foreach (var cf in transactions.NewCustomFormats)
        {
            try
            {
                var response = await api.CreateCustomFormat(cf, ct);
                if (response is null)
                {
                    incompleteResources++;
                    persistenceOutcomes.Add(new CustomFormatCreateRejectedOutcome(ToIdentity(cf)));
                    continue;
                }

                cf.Id = response.Id;
                computeResult.RecordSynced(cf);
                completedResources++;
            }
            catch (ApiException e) when (IsResourceRejection(e))
            {
                incompleteResources++;
                persistenceOutcomes.Add(new CustomFormatCreateRejectedOutcome(ToIdentity(cf)));
            }
        }

        foreach (var cf in transactions.UpdatedCustomFormats)
        {
            try
            {
                await api.UpdateCustomFormat(cf, ct);
                computeResult.RecordSynced(cf);
                completedResources++;
            }
            catch (ApiException e) when (IsResourceRejection(e))
            {
                incompleteResources++;
                persistenceOutcomes.Add(new CustomFormatUpdateRejectedOutcome(ToIdentity(cf)));
            }
        }

        foreach (var mapping in transactions.DeletedCustomFormats)
        {
            try
            {
                await api.DeleteCustomFormat(mapping.ServiceId, ct);
                computeResult.RecordDeleted(mapping.ServiceId);
                completedResources++;
            }
            catch (ApiException e) when (IsResourceRejection(e))
            {
                incompleteResources++;
                persistenceOutcomes.Add(new CustomFormatDeleteRejectedOutcome(ToIdentity(mapping)));
            }
        }

        computeResult.State.Update(computeResult);
        statePersister.Save(computeResult.State);
        computeResult.CompleteApply(completedResources, incompleteResources, persistenceOutcomes);
        CustomFormatTransactionLogger.SetStatus(
            computeResult.Transactions,
            publisher,
            computeResult.Result
        );
    }

    private static bool IsResourceRejection(ApiException exception)
    {
        return exception.StatusCode
            is HttpStatusCode.BadRequest
                or HttpStatusCode.NotFound
                or HttpStatusCode.Conflict
                or HttpStatusCode.UnprocessableEntity;
    }

    private void ProcessCachedCf(
        CustomFormatResource guideCf,
        int storedId,
        Dictionary<int, CustomFormatResource> serviceCfsById,
        ILookup<string, CustomFormatResource> serviceCfsByName,
        CustomFormatTransactionData transactions,
        CustomFormatSourceInfo provenance,
        ICollection<CustomFormatOutcome> outcomes,
        ICollection<CustomFormatDelta> deltas
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

            AddUpdatedOrUnchanged(guideCf, serviceCf, transactions, provenance, deltas);
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
            ProcessNameCollision(
                guideCf,
                serviceCfsByName,
                transactions,
                provenance,
                outcomes,
                deltas
            );
        }
    }

    private static void ProcessUncachedCf(
        CustomFormatResource guideCf,
        ILookup<string, CustomFormatResource> serviceCfsByName,
        CustomFormatTransactionData transactions,
        CustomFormatSourceInfo provenance,
        ICollection<CustomFormatOutcome> outcomes,
        ICollection<CustomFormatDelta> deltas
    )
    {
        ProcessNameCollision(guideCf, serviceCfsByName, transactions, provenance, outcomes, deltas);
    }

    private static void ProcessNameCollision(
        CustomFormatResource guideCf,
        ILookup<string, CustomFormatResource> serviceCfsByName,
        CustomFormatTransactionData transactions,
        CustomFormatSourceInfo provenance,
        ICollection<CustomFormatOutcome> outcomes,
        ICollection<CustomFormatDelta> deltas
    )
    {
        var nameMatches = serviceCfsByName[guideCf.Name].ToList();

        switch (nameMatches.Count)
        {
            case 0:
                // No collision - safe to create
                transactions.NewCustomFormats.Add(guideCf);
                deltas.Add(new CustomFormatCreateDelta(ToIdentity(guideCf), provenance));
                break;

            case 1:
                // Config is authoritative: adopt the existing service CF
                guideCf.Id = nameMatches[0].Id;
                transactions.ReplacedCustomFormats.Add(guideCf.Name);
                outcomes.Add(new CustomFormatAdoptedOutcome(ToIdentity(guideCf), guideCf.Id));
                AddUpdatedOrUnchanged(guideCf, nameMatches[0], transactions, provenance, deltas);
                break;

            default:
                // Multiple matches - ambiguous
                transactions.AmbiguousCustomFormats.Add(
                    new AmbiguousMatch(
                        guideCf.Name,
                        nameMatches.Select(cf => (cf.Name, cf.Id)).ToList()
                    )
                );
                outcomes.Add(
                    new CustomFormatAmbiguousMatchOutcome(
                        ToIdentity(guideCf),
                        nameMatches
                            .Select(cf => new CustomFormatServiceMatch(cf.Name, cf.Id))
                            .ToList()
                    )
                );
                break;
        }
    }

    private static void AddUpdatedOrUnchanged(
        CustomFormatResource guideCf,
        CustomFormatResource serviceCf,
        CustomFormatTransactionData transactions,
        CustomFormatSourceInfo provenance,
        ICollection<CustomFormatDelta> deltas
    )
    {
        if (!IsEquivalent(guideCf, serviceCf))
        {
            transactions.UpdatedCustomFormats.Add(guideCf);
            deltas.Add(
                new CustomFormatUpdateDelta(
                    ToIdentity(guideCf),
                    provenance,
                    BuildUpdateComponents(serviceCf, guideCf)
                )
            );
        }
        else
        {
            transactions.UnchangedCustomFormats.Add(guideCf);
        }
    }

    private static List<CustomFormatUpdateComponent> BuildUpdateComponents(
        CustomFormatResource current,
        CustomFormatResource desired
    )
    {
        var components = new List<CustomFormatUpdateComponent>();
        if (!string.Equals(current.Name, desired.Name, StringComparison.Ordinal))
        {
            components.Add(
                new CustomFormatNameChanged(new ValueDelta<string>(current.Name, desired.Name))
            );
        }

        if (current.IncludeCustomFormatWhenRenaming != desired.IncludeCustomFormatWhenRenaming)
        {
            components.Add(
                new CustomFormatIncludeWhenRenamingChanged(
                    new ValueDelta<bool>(
                        current.IncludeCustomFormatWhenRenaming,
                        desired.IncludeCustomFormatWhenRenaming
                    )
                )
            );
        }

        foreach (var desiredSpec in desired.Specifications)
        {
            var currentSpec = current.Specifications.FirstOrDefault(x =>
                x.Name == desiredSpec.Name
            );
            if (currentSpec is null)
            {
                components.Add(new CustomFormatSpecificationAdded(desiredSpec.Name));
                continue;
            }

            if (currentSpec != desiredSpec)
            {
                components.Add(new CustomFormatSpecificationChanged(desiredSpec.Name));
            }
        }

        components.AddRange(
            current
                .Specifications.Where(x => desired.Specifications.All(y => y.Name != x.Name))
                .Select(x => new CustomFormatSpecificationRemoved(x.Name))
        );
        return components;
    }

    private static IEnumerable<CustomFormatOutcome> MapPlanOutcomes(PipelinePlan plan)
    {
        foreach (var outcome in plan.Outcomes)
        {
            switch (outcome)
            {
                case InvalidCustomFormatTrashIdOutcome x:
                    yield return new CustomFormatReferenceMismatchOutcome(x.TrashId);
                    break;
                case InvalidCfGroupSkipIdOutcome x:
                    yield return new CustomFormatGroupReferenceMismatchOutcome(x.TrashId);
                    break;
                case IncompatibleCfGroupOutcome x:
                    yield return new IncompatibleCustomFormatGroupOutcome(x.Name, x.TrashId);
                    break;
                case EmptyCfGroupOutcome x:
                    yield return new EmptyCustomFormatGroupOutcome(x.Name, x.TrashId);
                    break;
            }
        }
    }

    private static CustomFormatSourceInfo CreateSourceInfo(PlannedCustomFormat cf)
    {
        return new CustomFormatSourceInfo(
            cf.Source,
            cf.GroupName,
            cf.InclusionReason,
            cf.AssignScoresTo.Select(x => x.Name).ToList()
        );
    }

    private static CustomFormatIdentity ToIdentity(CustomFormatResource cf)
    {
        return new CustomFormatIdentity(cf.TrashId, cf.Name);
    }

    private static CustomFormatIdentity ToIdentity(TrashIdMapping mapping)
    {
        return new CustomFormatIdentity(mapping.TrashId, mapping.Name);
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
