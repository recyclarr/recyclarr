using TrashLib.Sonarr.Config;

namespace TrashLib.Sonarr.ReleaseProfile.Filters;

public interface IReleaseProfileFilterPipeline
{
    ReleaseProfileData Process(ReleaseProfileData profile, ReleaseProfileConfig config);
}
