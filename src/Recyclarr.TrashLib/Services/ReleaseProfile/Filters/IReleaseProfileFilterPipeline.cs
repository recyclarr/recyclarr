using Recyclarr.TrashLib.Services.Sonarr.Config;

namespace Recyclarr.TrashLib.Services.ReleaseProfile.Filters;

public interface IReleaseProfileFilterPipeline
{
    ReleaseProfileData Process(ReleaseProfileData profile, ReleaseProfileConfig config);
}
