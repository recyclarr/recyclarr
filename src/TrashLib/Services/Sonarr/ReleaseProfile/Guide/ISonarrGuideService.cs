using TrashLib.Services.Common;
using TrashLib.Services.Sonarr.QualityDefinition;

namespace TrashLib.Services.Sonarr.ReleaseProfile.Guide;

public interface ISonarrGuideService : IGuideService
{
    IReadOnlyCollection<ReleaseProfileData> GetReleaseProfileData();
    ReleaseProfileData? GetUnfilteredProfileById(string trashId);
    ICollection<SonarrQualityData> GetQualities();
}
