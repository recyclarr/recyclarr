using Recyclarr.TrashLib.Services.Common;

namespace Recyclarr.TrashLib.Services.Sonarr.ReleaseProfile.Guide;

public interface ISonarrGuideService : IGuideService
{
    IReadOnlyCollection<ReleaseProfileData> GetReleaseProfileData();
    ReleaseProfileData? GetUnfilteredProfileById(string trashId);
}
