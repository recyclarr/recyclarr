using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.Filters;

public interface IReleaseProfileFilter
{
    ReleaseProfileData Transform(ReleaseProfileData profile, ReleaseProfileConfig config);
}
