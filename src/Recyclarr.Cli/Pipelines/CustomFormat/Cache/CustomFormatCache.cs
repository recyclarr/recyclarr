using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Cache;

public class CustomFormatCache(IEnumerable<TrashIdMapping> mappings)
{
    private List<TrashIdMapping> _mappings = mappings.ToList(); // Deep clone with ToList()

    public IReadOnlyList<TrashIdMapping> Mappings => _mappings;

    public void Update(CustomFormatTransactionData transactions)
    {
        // Assume that RemoveStale() is called before this method, and that TrashIdMappings contains existing CFs
        // in the remote service that we want to keep and update.

        var existingCfs = transactions.UpdatedCustomFormats
            .Concat(transactions.UnchangedCustomFormats)
            .Concat(transactions.NewCustomFormats);

        _mappings = _mappings
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
                (l, r) => l with {TrashId = r.TrashId, CustomFormatName = r.Name})
            .Where(x => x.CustomFormatId != 0)
            .OrderBy(x => x.CustomFormatId)
            .ToList();
    }

    public void RemoveStale(IEnumerable<CustomFormatData> serviceCfs)
    {
        _mappings.RemoveAll(x => x.CustomFormatId == 0 || serviceCfs.All(y => y.Id != x.CustomFormatId));
    }

    public int? FindId(CustomFormatData cf)
    {
        return _mappings.Find(c => c.TrashId == cf.TrashId)?.CustomFormatId;
    }
}
