using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Radarr.CustomFormat.Models;

public class ProcessedCustomFormatData
{
    private readonly CustomFormatData _data;

    public ProcessedCustomFormatData(CustomFormatData data)
    {
        _data = data;
        Json = _data.ExtraJson;
    }

    public string Name => _data.Name;
    public string TrashId => _data.TrashId;
    public int? Score => _data.Score;
    public JObject Json { get; set; }
    public TrashIdMapping? CacheEntry { get; set; }
    public string CacheAwareName => CacheEntry?.CustomFormatName ?? Name;

    public void SetCache(int customFormatId)
    {
        CacheEntry ??= new TrashIdMapping(TrashId, Name);
        CacheEntry.CustomFormatId = customFormatId;
    }

    [SuppressMessage("Microsoft.Design", "CA1024", Justification = "Method throws an exception")]
    public int GetCustomFormatId()
        => CacheEntry?.CustomFormatId ??
           throw new InvalidOperationException("CacheEntry must exist to obtain custom format ID");
}
