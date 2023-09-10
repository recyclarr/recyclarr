namespace Recyclarr.TrashLib.Config.Parsing.PostProcessing.ConfigMerging;

public class SonarrConfigMerger : ServiceConfigMerger<SonarrConfigYaml>
{
    public override SonarrConfigYaml Merge(SonarrConfigYaml a, SonarrConfigYaml b)
    {
        return base.Merge(a, b) with
        {
            ReleaseProfiles = Combine(a.ReleaseProfiles, b.ReleaseProfiles,
                (x, y) => x.Concat(y).ToList())
        };
    }
}
