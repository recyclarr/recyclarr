using TrashLib.Services.Radarr.CustomFormat.Models;
using TrashLib.Services.Radarr.QualityDefinition;

namespace TrashLib.Services.Radarr.CustomFormat.Guide;

public interface IRadarrGuideService
{
    ICollection<CustomFormatData> GetCustomFormatData();
    ICollection<RadarrQualityData> GetQualities();
}
