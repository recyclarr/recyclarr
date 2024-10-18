using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

public class RadarrConfigMerger : ServiceConfigMerger<RadarrConfigYaml>
{
    public override RadarrConfigYaml Merge(RadarrConfigYaml a, RadarrConfigYaml b)
    {
        return base.Merge(a, b) with
        {
            MediaNaming = Combine(a.MediaNaming, b.MediaNaming, MergeMediaNaming)
        };
    }

    [SuppressMessage("ReSharper", "WithExpressionModifiesAllMembers")]
    private static RadarrMediaNamingConfigYaml MergeMediaNaming(
        RadarrMediaNamingConfigYaml a,
        RadarrMediaNamingConfigYaml b)
    {
        return a with
        {
            Folder = b.Folder ?? a.Folder,
            Movie = Combine(a.Movie, b.Movie, (a1, b1) => a1 with
            {
                Rename = b1.Rename ?? a1.Rename,
                Standard = b1.Standard ?? a1.Standard
            })
        };
    }
}
