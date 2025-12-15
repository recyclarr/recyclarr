using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.Common.Extensions;
using Recyclarr.Config.Models;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Pipelines.CustomFormat.PipelinePhases;

internal class CustomFormatTransactionPhase(ILogger log, IServiceConfiguration config)
    : IPipelinePhase<CustomFormatPipelineContext>
{
    public Task<PipelineFlow> Execute(CustomFormatPipelineContext context, CancellationToken ct)
    {
        var plannedCfs = context.Plan.CustomFormats;
        var transactions = new CustomFormatTransactionData();

        // Build lookups for O(1) access
        var serviceCfsById = context.ApiFetchOutput.ToDictionary(cf => cf.Id);
        var serviceCfsByName = context.ApiFetchOutput.ToLookup(
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

            var cachedId = context.Cache.FindId(guideCf);

            if (cachedId.HasValue)
            {
                ProcessCachedCf(
                    guideCf,
                    cachedId.Value,
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

        if (config.DeleteOldCustomFormats)
        {
            transactions.DeletedCustomFormats.AddRange(
                context
                    .Cache.Mappings
                    // Custom format must be in the cache but NOT in the user's config
                    .Where(map => plannedCfs.All(cf => cf.Resource.TrashId != map.TrashId))
                    // Also, that cache-only CF must exist in the service (otherwise there is
                    // nothing to delete)
                    .Where(map => serviceCfsById.ContainsKey(map.ServiceId))
            );
        }

        context.TransactionOutput = transactions;
        return Task.FromResult(PipelineFlow.Continue);
    }

    private void ProcessCachedCf(
        CustomFormatResource guideCf,
        int cachedId,
        Dictionary<int, CustomFormatResource> serviceCfsById,
        ILookup<string, CustomFormatResource> serviceCfsByName,
        CustomFormatTransactionData transactions
    )
    {
        if (serviceCfsById.TryGetValue(cachedId, out var serviceCf))
        {
            // ID-first: Found by cached ID - update regardless of name
            guideCf.Id = cachedId;

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
            // Stale cache: cached ID no longer exists in service
            log.Debug(
                "Cached service ID {CachedId} for CF {TrashId} no longer exists in service",
                cachedId,
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
                // Single match - conflict (user must run cache rebuild --adopt)
                transactions.ConflictingCustomFormats.Add(
                    new ConflictingCustomFormat(guideCf, nameMatches[0].Id)
                );
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
