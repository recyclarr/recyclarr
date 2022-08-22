using TrashLib.Services.Sonarr.Config;

namespace TrashLib.Services.Sonarr.ReleaseProfile.Filters;

public interface IReleaseProfileFilterPipeline
{
    ReleaseProfileData Process(ReleaseProfileData profile, ReleaseProfileConfig config);
}
