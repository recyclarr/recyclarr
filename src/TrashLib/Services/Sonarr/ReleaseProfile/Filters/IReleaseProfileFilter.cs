using TrashLib.Services.Sonarr.Config;

namespace TrashLib.Services.Sonarr.ReleaseProfile.Filters;

public interface IReleaseProfileFilter
{
    ReleaseProfileData Transform(ReleaseProfileData profile, ReleaseProfileConfig config);
}
