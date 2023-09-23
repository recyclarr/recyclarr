using Recyclarr.Common.Extensions;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Models;

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
        return _customFormats.Find(x => x.TrashId.EqualsIgnoreCase(trashId));
    }

    public CustomFormatData? LookupByServiceId(int id)
    {
        return _customFormats.Find(x => x.Id == id);
    }
}
