using TrashLib.Sonarr.Config;

namespace TrashLib.Sonarr.ReleaseProfile.Filters;

public class ReleaseProfileFilterPipeline : IReleaseProfileFilterPipeline
{
    private readonly IOrderedEnumerable<IReleaseProfileFilter> _filters;

    public ReleaseProfileFilterPipeline(IOrderedEnumerable<IReleaseProfileFilter> filters)
    {
        _filters = filters;
    }

    public ReleaseProfileData Process(ReleaseProfileData profile, ReleaseProfileConfig config)
    {
        foreach (var filter in _filters)
        {
            profile = filter.Transform(profile, config);
        }

        return profile;
    }
}
