namespace Recyclarr.Cli.Pipelines.ReleaseProfile.Guide;

public interface IReleaseProfileGuideService
{
    IReadOnlyList<ReleaseProfileData> GetReleaseProfileData();
}
