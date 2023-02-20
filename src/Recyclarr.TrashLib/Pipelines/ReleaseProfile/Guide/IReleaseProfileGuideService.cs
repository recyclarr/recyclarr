namespace Recyclarr.TrashLib.Pipelines.ReleaseProfile.Guide;

public interface IReleaseProfileGuideService
{
    IReadOnlyList<ReleaseProfileData> GetReleaseProfileData();
}
