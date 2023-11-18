using Recyclarr.Config.Models;
using Recyclarr.TrashGuide.ReleaseProfile;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.Filters;

public class StrictNegativeScoresFilter(ILogger log) : IReleaseProfileFilter
{
    public ReleaseProfileData Transform(ReleaseProfileData profile, ReleaseProfileConfig config)
    {
        if (!config.StrictNegativeScores)
        {
            return profile;
        }

        log.Debug("Negative scores will be strictly ignored");
        var splitPreferred = profile.Preferred.ToLookup(x => x.Score < 0);

        return profile with
        {
            Ignored = profile.Ignored.Concat(splitPreferred[true].SelectMany(x => x.Terms)).ToList(),
            Preferred = splitPreferred[false].ToList()
        };
    }
}
