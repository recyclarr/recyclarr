using Recyclarr.TrashLib.Config.Services.Sonarr;

namespace Recyclarr.TrashLib.Pipelines.ReleaseProfile.Filters;

public interface IReleaseProfileFilterPipeline
{
    ReleaseProfileData Process(ReleaseProfileData profile, ReleaseProfileConfig config);
}
