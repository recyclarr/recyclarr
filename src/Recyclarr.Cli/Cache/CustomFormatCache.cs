using Recyclarr.TrashLib.Models;

namespace Recyclarr.Cli.Cache;

[CacheObjectName("custom-format-cache")]
public record CustomFormatCache
{
    public const int LatestVersion = 1;

    public int Version { get; init; } = LatestVersion;
    public IReadOnlyList<TrashIdMapping> TrashIdMappings { get; init; } = new List<TrashIdMapping>();

    public CustomFormatCache Update(IEnumerable<CustomFormatData> customFormats)
    {
        return this with
        {
            TrashIdMappings = customFormats
                .Where(cf => cf.Id is not 0)
                .Select(cf => new TrashIdMapping(cf.TrashId, cf.Name, cf.Id))
                .ToList()
        };
    }

    public CustomFormatCache RemoveStale(IEnumerable<CustomFormatData> serviceCfs)
    {
        return this with
        {
            TrashIdMappings = TrashIdMappings
                .Where(x => serviceCfs.Any(y => y.Id == x.CustomFormatId))
                .ToList()
        };
    }

    public int? FindId(CustomFormatData cf)
    {
        return TrashIdMappings.FirstOrDefault(c => c.TrashId == cf.TrashId)?.CustomFormatId;
    }
}

public record TrashIdMapping(string TrashId, string CustomFormatName, int CustomFormatId);
