using Recyclarr.Config.Models;
using Recyclarr.TrashGuide.ReleaseProfile;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.Filters;

public class IncludeExcludeFilter(ILogger log) : IReleaseProfileFilter
{
    private readonly ReleaseProfileDataFilterer _filterer = new(log);

    public ReleaseProfileData Transform(ReleaseProfileData profile, ReleaseProfileConfig config)
    {
        if (config.Filter == null)
        {
            return profile;
        }

        log.Debug("This profile will be filtered");
        var newProfile = _filterer.FilterProfile(profile, config.Filter);
        return newProfile ?? profile;
    }
}
