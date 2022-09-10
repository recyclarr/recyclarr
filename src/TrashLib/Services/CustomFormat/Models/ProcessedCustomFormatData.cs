using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;
using TrashLib.Services.CustomFormat.Models.Cache;

namespace TrashLib.Services.CustomFormat.Models;

public class ProcessedCustomFormatData
{
    private readonly CustomFormatData _data;

    public ProcessedCustomFormatData(CustomFormatData data)
    {
        _data = data;
        Json = _data.Json;
    }

    public string Name => _data.Name;
    public string TrashId => _data.TrashId;
    public int? Score => _data.Score;
    public JObject Json { get; set; }
    public TrashIdMapping? CacheEntry { get; set; }

    public void SetCache(int customFormatId)
    {
        CacheEntry ??= new TrashIdMapping(TrashId);
        CacheEntry.CustomFormatId = customFormatId;
    }

    [SuppressMessage("Microsoft.Design", "CA1024", Justification = "Method throws an exception")]
    public int GetCustomFormatId()
        => CacheEntry?.CustomFormatId ??
           throw new InvalidOperationException("CacheEntry must exist to obtain custom format ID");
}
