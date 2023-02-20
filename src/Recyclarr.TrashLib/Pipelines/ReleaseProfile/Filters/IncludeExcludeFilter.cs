using Recyclarr.TrashLib.Config.Services.Sonarr;

namespace Recyclarr.TrashLib.Pipelines.ReleaseProfile.Filters;

public class IncludeExcludeFilter : IReleaseProfileFilter
{
    private readonly ILogger _log;
    private readonly ReleaseProfileDataFilterer _filterer;

    public IncludeExcludeFilter(ILogger log)
    {
        _log = log;
        _filterer = new ReleaseProfileDataFilterer(log);
    }

    public ReleaseProfileData Transform(ReleaseProfileData profile, ReleaseProfileConfig config)
    {
        if (config.Filter == null)
        {
            return profile;
        }

        _log.Debug("This profile will be filtered");
        var newProfile = _filterer.FilterProfile(profile, config.Filter);
        return newProfile ?? profile;
    }
}
