using System.Diagnostics.CodeAnalysis;
using Recyclarr.Cache;
using Recyclarr.Cli.Pipelines.CustomFormat.Models;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Cache;

internal record TrashIdMapping(string TrashId, string CustomFormatName, int CustomFormatId);

[CacheObjectName("custom-format-cache")]
[SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "POCO")]
[SuppressMessage(
    "Usage",
    "CA2227:Collection properties should be read only",
    Justification = "POCO"
)]
internal record CustomFormatCacheObject() : CacheObject(1)
{
    public List<TrashIdMapping> TrashIdMappings { get; set; } = [];
}

internal class CustomFormatCache(CustomFormatCacheObject cacheObject) : BaseCache(cacheObject)
{
    public IReadOnlyList<TrashIdMapping> TrashIdMappings => cacheObject.TrashIdMappings;

    public void Update(CustomFormatTransactionData transactions)
    {
        // Assume that RemoveStale() is called before this method, and that TrashIdMappings contains existing CFs
        // in the remote service that we want to keep and update.

        var existingCfs = transactions
            .UpdatedCustomFormats.Concat(transactions.UnchangedCustomFormats)
            .Concat(transactions.NewCustomFormats);

        cacheObject.TrashIdMappings = cacheObject
            .TrashIdMappings.DistinctBy(x => x.CustomFormatId)
            .Where(x =>
                transactions.DeletedCustomFormats.All(y => y.CustomFormatId != x.CustomFormatId)
            )
            .FullOuterHashJoin(
                existingCfs,
                l => l.CustomFormatId,
                r => r.Id,
                // Keep existing service CFs, even if they aren't in user config
                l => l,
                // Add a new mapping for CFs in user's config
                r => new TrashIdMapping(r.TrashId, r.Name, r.Id),
                // Update existing mappings for CFs in user's config
                (l, r) => l with { TrashId = r.TrashId, CustomFormatName = r.Name }
            )
            .Where(x => x.CustomFormatId != 0)
            .OrderBy(x => x.CustomFormatId)
            .ToList();
    }

    public void RemoveStale(IEnumerable<CustomFormatData> serviceCfs)
    {
        cacheObject.TrashIdMappings.RemoveAll(x =>
            x.CustomFormatId == 0 || serviceCfs.All(y => y.Id != x.CustomFormatId)
        );

        // Clean up duplicate IDs - keep first occurrence, remove the rest
        //
        // The reasons for duplicates are not known, but they screw up everything so this is the
        // opportunity to clean them up early before transaction processing.
        var duplicatesToRemove = cacheObject
            .TrashIdMappings.GroupBy(x => x.CustomFormatId)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Skip(1))
            .ToList();

        cacheObject.TrashIdMappings.RemoveAll(duplicatesToRemove.Contains);
    }

    public int? FindId(CustomFormatData cf)
    {
        return cacheObject.TrashIdMappings.Find(c => c.TrashId == cf.TrashId)?.CustomFormatId;
    }
}
