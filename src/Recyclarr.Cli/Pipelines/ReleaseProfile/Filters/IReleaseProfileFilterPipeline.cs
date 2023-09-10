using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Guide.ReleaseProfile;

namespace Recyclarr.Cli.Pipelines.ReleaseProfile.Filters;

public interface IReleaseProfileFilterPipeline
{
    ReleaseProfileData Process(ReleaseProfileData profile, ReleaseProfileConfig config);
}
