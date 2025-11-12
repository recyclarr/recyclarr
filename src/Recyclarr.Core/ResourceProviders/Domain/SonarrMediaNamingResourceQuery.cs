using Recyclarr.ResourceProviders.Infrastructure;

namespace Recyclarr.ResourceProviders.Domain;

public class SonarrMediaNamingResourceQuery(
    IResourcePathRegistry registry,
    JsonResourceLoader loader
)
{
    public SonarrMediaNamingResource GetNaming()
    {
        var files = registry.GetFiles<SonarrMediaNamingResource>();
        var allData = loader.Load<SonarrMediaNamingResource>(files).Select(tuple => tuple.Resource);

        return new SonarrMediaNamingResource
        {
            Season = MergeDictionaries(allData.Select(x => x.Season)),
            Series = MergeDictionaries(allData.Select(x => x.Series)),
            Episodes = new SonarrEpisodeNamingResource
            {
                Anime = MergeDictionaries(allData.Select(x => x.Episodes.Anime)),
                Daily = MergeDictionaries(allData.Select(x => x.Episodes.Daily)),
                Standard = MergeDictionaries(allData.Select(x => x.Episodes.Standard)),
            },
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
