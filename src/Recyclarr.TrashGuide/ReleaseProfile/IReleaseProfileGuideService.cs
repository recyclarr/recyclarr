namespace Recyclarr.TrashGuide.ReleaseProfile;

public interface IReleaseProfileGuideService
{
    IReadOnlyList<ReleaseProfileData> GetReleaseProfileData();
}
