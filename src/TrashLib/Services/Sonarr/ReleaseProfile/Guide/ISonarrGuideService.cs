using TrashLib.Services.Common;

namespace TrashLib.Services.Sonarr.ReleaseProfile.Guide;

public interface ISonarrGuideService : IGuideService
{
    IReadOnlyCollection<ReleaseProfileData> GetReleaseProfileData();
    ReleaseProfileData? GetUnfilteredProfileById(string trashId);
}
