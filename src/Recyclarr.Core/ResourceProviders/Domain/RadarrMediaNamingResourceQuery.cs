using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.ResourceProviders.Domain;

public class RadarrMediaNamingResourceQuery(
    IResourcePathRegistry registry,
    JsonResourceLoader loader
)
{
    public RadarrMediaNamingResource GetNaming()
    {
        var files = registry.GetFiles<RadarrMediaNamingResource>();
        var allData = loader.Load<RadarrMediaNamingResource>(files).Select(tuple => tuple.Resource);

        return new RadarrMediaNamingResource
        {
            File = MergeDictionaries(allData.Select(x => x.File)),
            Folder = MergeDictionaries(allData.Select(x => x.Folder)),
        };
    }

    private static Dictionary<string, string> MergeDictionaries(
        IEnumerable<IReadOnlyDictionary<string, string>> dicts
    )
    {
        return dicts
            .SelectMany(d => d)
            .GroupBy(kvp => kvp.Key.ToLowerInvariant())
            .Select(g => g.Last())
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
