using Recyclarr.Cli.Pipelines.CustomFormat;
using Recyclarr.TrashLib.Models;

namespace Recyclarr.Cli.Cache;

[CacheObjectName("custom-format-cache")]
public record CustomFormatCache
{
    public const int LatestVersion = 1;

    public int Version { get; init; } = LatestVersion;
    public IReadOnlyList<TrashIdMapping> TrashIdMappings { get; init; } = new List<TrashIdMapping>();

    public CustomFormatCache Update(CustomFormatTransactionData transactions)
    {
        // Assume that RemoveStale() is called before this method, and that TrashIdMappings contains existing CFs
        // in the remote service that we want to keep and update.

        var existingCfs = transactions.UpdatedCustomFormats
            .Concat(transactions.UnchangedCustomFormats)
            .Concat(transactions.NewCustomFormats);

        return this with
        {
            TrashIdMappings = TrashIdMappings
                .DistinctBy(x => x.CustomFormatId)
                .Where(x => transactions.DeletedCustomFormats.All(y => y.CustomFormatId != x.CustomFormatId))
                .FullOuterJoin(existingCfs, JoinType.Hash,
                    l => l.CustomFormatId,
                    r => r.Id,
                    // Keep existing service CFs, even if they aren't in user config
                    l => l,
                    // Add a new mapping for CFs in user's config
                    r => new TrashIdMapping(r.TrashId, r.Name, r.Id),
                    // Update existing mappings for CFs in user's config
                    (l, r) => l with { TrashId = r.TrashId, CustomFormatName = r.Name })
                .Where(x => x.CustomFormatId != 0)
                .OrderBy(x => x.CustomFormatId)
                .ToList()
        };
    }

    public CustomFormatCache RemoveStale(IEnumerable<CustomFormatData> serviceCfs)
    {
        return this with
        {
            TrashIdMappings = TrashIdMappings
                .Where(x => x.CustomFormatId != 0 && serviceCfs.Any(y => y.Id == x.CustomFormatId))
                .ToList()
        };
    }

    public int? FindId(CustomFormatData cf)
    {
        return TrashIdMappings.FirstOrDefault(c => c.TrashId == cf.TrashId)?.CustomFormatId;
    }
}

public record TrashIdMapping(string TrashId, string CustomFormatName, int CustomFormatId);
