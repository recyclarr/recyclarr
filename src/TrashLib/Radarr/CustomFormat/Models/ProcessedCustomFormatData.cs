using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;
using TrashLib.Radarr.CustomFormat.Models.Cache;

namespace TrashLib.Radarr.CustomFormat.Models;

public class ProcessedCustomFormatData
{
    public ProcessedCustomFormatData(string name, string trashId, JObject json)
    {
        Name = name;
        TrashId = trashId;
        Json = json;
    }

    public string Name { get; }
    public string TrashId { get; }
    public int? Score { get; init; }
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
