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

internal class CustomFormatCache(CustomFormatCacheObject cacheObject, ILogger logger) : BaseCache(cacheObject)
{
    public IReadOnlyList<TrashIdMapping> TrashIdMappings => cacheObject.TrashIdMappings;

    private void LogCacheState(string context)
    {
        logger.Debug("Cache state at {Context}: {Count} mappings", context, cacheObject.TrashIdMappings.Count);
        foreach (var mapping in cacheObject.TrashIdMappings)
        {
            logger.Debug("  - {TrashId} ({Name}) -> ID {Id}", mapping.TrashId, mapping.CustomFormatName, mapping.CustomFormatId);
        }
        
        var duplicateGroups = cacheObject.TrashIdMappings.GroupBy(x => x.CustomFormatId).Where(g => g.Count() > 1);
        foreach (var group in duplicateGroups)
        {
            logger.Warning("  DUPLICATE ID {Id}: {Count} entries", group.Key, group.Count());
        }
    }

    public void Update(CustomFormatTransactionData transactions)
    {
        LogCacheState("Update - BEFORE");
        
        // Assume that RemoveStale() is called before this method, and that TrashIdMappings contains existing CFs
        // in the remote service that we want to keep and update.

        var existingCfs = transactions
            .UpdatedCustomFormats.Concat(transactions.UnchangedCustomFormats)
            .Concat(transactions.NewCustomFormats);

        logger.Debug("Update: Processing {UpdatedCount} updated, {UnchangedCount} unchanged, {NewCount} new CFs",
            transactions.UpdatedCustomFormats.Count, 
            transactions.UnchangedCustomFormats.Count, 
            transactions.NewCustomFormats.Count);
            
        foreach (var cf in existingCfs)
        {
            logger.Debug("  Transaction CF: {TrashId} ({Name}) -> ID {Id}", cf.TrashId, cf.Name, cf.Id);
        }

        var beforeDistinct = cacheObject.TrashIdMappings.ToList();
        var afterDistinct = beforeDistinct.DistinctBy(x => x.CustomFormatId).ToList();
        
        if (beforeDistinct.Count != afterDistinct.Count)
        {
            logger.Warning("DistinctBy removed {Removed} duplicate mappings ({Before} -> {After})", 
                beforeDistinct.Count - afterDistinct.Count, beforeDistinct.Count, afterDistinct.Count);
                
            var removedIds = beforeDistinct.GroupBy(x => x.CustomFormatId).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var removedId in removedIds)
            {
                var duplicates = beforeDistinct.Where(x => x.CustomFormatId == removedId).ToList();
                logger.Debug("  Removed duplicates for ID {Id}:", removedId);
                foreach (var dup in duplicates.Skip(1))
                {
                    logger.Debug("    Removed: {TrashId} ({Name}) -> ID {Id}", dup.TrashId, dup.CustomFormatName, dup.CustomFormatId);
                }
            }
        }

        logger.Debug("About to join - LEFT side (cache after DistinctBy): {Count} entries", afterDistinct.Count);
        foreach (var left in afterDistinct)
        {
            logger.Debug("  LEFT: {TrashId} ({Name}) -> ID {Id}", left.TrashId, left.CustomFormatName, left.CustomFormatId);
        }

        logger.Debug("About to join - RIGHT side (transaction CFs): {Count} entries", existingCfs.Count());
        foreach (var right in existingCfs)
        {
            logger.Debug("  RIGHT: {TrashId} ({Name}) -> ID {Id}", right.TrashId, right.Name, right.Id);
        }

        var duplicateTransactionIds = existingCfs.GroupBy(x => x.Id).Where(g => g.Count() > 1);
        foreach (var group in duplicateTransactionIds)
        {
            logger.Warning("  DUPLICATE TRANSACTION ID {Id}: {Count} entries", group.Key, group.Count());
            foreach (var dup in group)
            {
                logger.Warning("    - {TrashId} ({Name}) -> ID {Id}", dup.TrashId, dup.Name, dup.Id);
            }
        }

        cacheObject.TrashIdMappings = afterDistinct
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
            
        LogCacheState("Update - AFTER");
    }

    public void RemoveStale(IEnumerable<CustomFormatData> serviceCfs)
    {
        LogCacheState("RemoveStale - BEFORE");
        
        var beforeCount = cacheObject.TrashIdMappings.Count;
        cacheObject.TrashIdMappings.RemoveAll(x =>
            x.CustomFormatId == 0 || serviceCfs.All(y => y.Id != x.CustomFormatId)
        );
        
        var afterCount = cacheObject.TrashIdMappings.Count;
        if (beforeCount != afterCount)
        {
            logger.Debug("RemoveStale removed {Removed} mappings ({Before} -> {After})", beforeCount - afterCount, beforeCount, afterCount);
        }
        
        LogCacheState("RemoveStale - AFTER");
    }

    public int? FindId(CustomFormatData cf)
    {
        return cacheObject.TrashIdMappings.Find(c => c.TrashId == cf.TrashId)?.CustomFormatId;
    }
}
