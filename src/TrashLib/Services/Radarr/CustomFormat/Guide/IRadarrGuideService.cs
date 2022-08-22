using TrashLib.Services.Radarr.CustomFormat.Models;

namespace TrashLib.Services.Radarr.CustomFormat.Guide;

public interface IRadarrGuideService
{
    ICollection<CustomFormatData> GetCustomFormatData();
}
