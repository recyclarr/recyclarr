using Recyclarr.TrashLib.Config.Services.Sonarr;

namespace Recyclarr.TrashLib.Pipelines.ReleaseProfile.Filters;

public interface IReleaseProfileFilter
{
    ReleaseProfileData Transform(ReleaseProfileData profile, ReleaseProfileConfig config);
}
