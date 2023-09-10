using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Guide.ReleaseProfile;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.Filters;

public class ReleaseProfileFilterPipeline : IReleaseProfileFilterPipeline
{
    private readonly IOrderedEnumerable<IReleaseProfileFilter> _filters;

    public ReleaseProfileFilterPipeline(IOrderedEnumerable<IReleaseProfileFilter> filters)
    {
        _filters = filters;
    }

    public ReleaseProfileData Process(ReleaseProfileData profile, ReleaseProfileConfig config)
    {
        return _filters.Aggregate(profile, (current, filter) => filter.Transform(current, config));
    }
}
