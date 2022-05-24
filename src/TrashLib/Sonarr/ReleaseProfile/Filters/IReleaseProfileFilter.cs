using TrashLib.Sonarr.Config;

namespace TrashLib.Sonarr.ReleaseProfile.Filters;

public interface IReleaseProfileFilter
{
    ReleaseProfileData Transform(ReleaseProfileData profile, ReleaseProfileConfig config);
}
