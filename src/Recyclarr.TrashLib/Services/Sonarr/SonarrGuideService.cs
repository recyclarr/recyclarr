using Recyclarr.TrashLib.Services.Common;
using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.QualitySize;
using Recyclarr.TrashLib.Services.Sonarr.ReleaseProfile;

namespace Recyclarr.TrashLib.Services.Sonarr;

public abstract class SonarrGuideService : IGuideService
{
    public abstract IReadOnlyCollection<ReleaseProfileData> GetReleaseProfileData();
    public abstract ReleaseProfileData? GetUnfilteredProfileById(string trashId);
    public abstract ICollection<CustomFormatData> GetCustomFormatData();
    public abstract ICollection<QualitySizeData> GetQualities();
}
