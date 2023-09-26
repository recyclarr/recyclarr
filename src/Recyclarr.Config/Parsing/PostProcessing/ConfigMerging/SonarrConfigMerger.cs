using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

public class SonarrConfigMerger : ServiceConfigMerger<SonarrConfigYaml>
{
    public override SonarrConfigYaml Merge(SonarrConfigYaml a, SonarrConfigYaml b)
    {
        return base.Merge(a, b) with
        {
            ReleaseProfiles = Combine(a.ReleaseProfiles, b.ReleaseProfiles,
                (x, y) => x.Concat(y).ToList()),

            MediaNaming = Combine(a.MediaNaming, b.MediaNaming, MergeMediaNaming)
        };
    }

    [SuppressMessage("ReSharper", "WithExpressionModifiesAllMembers")]
    private static SonarrMediaNamingConfigYaml MergeMediaNaming(
        SonarrMediaNamingConfigYaml a,
        SonarrMediaNamingConfigYaml b)
    {
        return a with
        {
            Series = b.Series ?? a.Series,
            Season = b.Season ?? a.Season,
            Episodes = Combine(a.Episodes, b.Episodes, (a1, b1) => a1 with
            {
                Rename = b1.Rename ?? a1.Rename,
                Standard = b1.Standard ?? a1.Standard,
                Daily = b1.Daily ?? a1.Daily,
                Anime = b1.Anime ?? a1.Anime
            })
        };
    }
}
