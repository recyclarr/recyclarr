using TrashLib.Services.CustomFormat.Models;
using TrashLib.Services.Radarr.QualityDefinition;

namespace TrashLib.Services.Radarr;

public interface IRadarrGuideService
{
    ICollection<CustomFormatData> GetCustomFormatData();
    ICollection<RadarrQualityData> GetQualities();
}
