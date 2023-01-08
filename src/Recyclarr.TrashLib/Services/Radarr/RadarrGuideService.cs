using Recyclarr.TrashLib.Services.Common;
using Recyclarr.TrashLib.Services.CustomFormat.Models;
using Recyclarr.TrashLib.Services.QualitySize;

namespace Recyclarr.TrashLib.Services.Radarr;

public abstract class RadarrGuideService : IGuideService
{
    public abstract ICollection<CustomFormatData> GetCustomFormatData();
    public abstract ICollection<QualitySizeData> GetQualities();
}
