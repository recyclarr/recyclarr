using TrashLib.Services.Common;
using TrashLib.Services.Radarr.QualityDefinition;

namespace TrashLib.Services.Radarr;

public interface IRadarrGuideService : IGuideService
{
    ICollection<RadarrQualityData> GetQualities();
}
