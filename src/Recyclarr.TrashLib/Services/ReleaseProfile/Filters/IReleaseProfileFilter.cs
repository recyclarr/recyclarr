using Recyclarr.TrashLib.Services.Sonarr.Config;

namespace Recyclarr.TrashLib.Services.ReleaseProfile.Filters;

public interface IReleaseProfileFilter
{
    ReleaseProfileData Transform(ReleaseProfileData profile, ReleaseProfileConfig config);
}
