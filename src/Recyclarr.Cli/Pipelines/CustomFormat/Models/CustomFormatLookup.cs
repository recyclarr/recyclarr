using Recyclarr.Common.Extensions;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Models;

internal class CustomFormatLookup : IPipelineCache
{
    private readonly List<CustomFormatResource> _customFormats = [];

    public void AddCustomFormats(IEnumerable<CustomFormatResource> customFormats)
    {
        _customFormats.AddRange(customFormats);
    }

    public void Clear()
    {
        _customFormats.Clear();
    }

    public CustomFormatResource? LookupByTrashId(string trashId)
    {
        return _customFormats.Find(x => x.TrashId.EqualsIgnoreCase(trashId));
    }

    public CustomFormatResource? LookupByServiceId(int id)
    {
        return _customFormats.Find(x => x.Id == id);
    }
}
