namespace TrashLib.Radarr.CustomFormat.Guide;

public interface IRadarrGuideService
{
    IEnumerable<string> GetCustomFormatJson();
}
