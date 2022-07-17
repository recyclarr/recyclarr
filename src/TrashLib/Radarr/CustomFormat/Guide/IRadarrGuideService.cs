using TrashLib.Radarr.CustomFormat.Models;

namespace TrashLib.Radarr.CustomFormat.Guide;

public interface IRadarrGuideService
{
    IObservable<CustomFormatData> GetCustomFormatData();
}
