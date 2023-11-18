using Recyclarr.Config.Models;
using Recyclarr.TrashGuide.ReleaseProfile;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.Filters;

public class ReleaseProfileFilterPipeline(IOrderedEnumerable<IReleaseProfileFilter> filters)
    : IReleaseProfileFilterPipeline
{
    public ReleaseProfileData Process(ReleaseProfileData profile, ReleaseProfileConfig config)
    {
        return filters.Aggregate(profile, (current, filter) => filter.Transform(current, config));
    }
}
