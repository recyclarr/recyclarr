using Recyclarr.Common.Extensions;
using Recyclarr.ServarrApi.Dto;

namespace Recyclarr.Cli.Pipelines.Tags;

public class ServiceTagCache : IPipelineCache
{
    private readonly HashSet<SonarrTag> _serviceTags = new();

    public IEnumerable<SonarrTag> Tags => _serviceTags;

    public void AddTags(IEnumerable<SonarrTag> tags)
    {
        _serviceTags.AddRange(tags);
    }

    public int? GetTagIdByName(string name)
    {
        var foundTag = _serviceTags.FirstOrDefault(x => x.Label.EqualsIgnoreCase(name));
        return foundTag?.Id;
    }

    public void Clear()
    {
        _serviceTags.Clear();
    }
}
