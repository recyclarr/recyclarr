using Serilog;
using TrashLib.Services.Sonarr.Config;

namespace TrashLib.Services.Sonarr.ReleaseProfile.Filters;

public class StrictNegativeScoresFilter : IReleaseProfileFilter
{
    private readonly ILogger _log;

    public StrictNegativeScoresFilter(ILogger log)
    {
        _log = log;
    }

    public ReleaseProfileData Transform(ReleaseProfileData profile, ReleaseProfileConfig config)
    {
        if (!config.StrictNegativeScores)
        {
            return profile;
        }

        _log.Debug("Negative scores will be strictly ignored");
        var splitPreferred = profile.Preferred.ToLookup(x => x.Score < 0);

        return profile with
        {
            Ignored = profile.Ignored.Concat(splitPreferred[true].SelectMany(x => x.Terms)).ToList(),
            Preferred = splitPreferred[false].ToList()
        };
    }
}
