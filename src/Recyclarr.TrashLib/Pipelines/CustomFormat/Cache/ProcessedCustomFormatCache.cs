using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Pipelines.CustomFormat.Models;

namespace Recyclarr.TrashLib.Pipelines.CustomFormat.Cache;

public class ProcessedCustomFormatCache : IPipelineCache
{
    private readonly List<CustomFormatData> _customFormats = new();

    public void AddCustomFormats(IEnumerable<CustomFormatData> customFormats)
    {
        _customFormats.AddRange(customFormats);
    }

    public void Clear()
    {
        _customFormats.Clear();
    }

    public CustomFormatData? LookupByTrashId(string trashId)
    {
        return _customFormats.FirstOrDefault(x => x.TrashId.EqualsIgnoreCase(trashId));
    }
}
