using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.Filters;

public interface IReleaseProfileFilterPipeline
{
    ReleaseProfileData Process(ReleaseProfileData profile, ReleaseProfileConfig config);
}
