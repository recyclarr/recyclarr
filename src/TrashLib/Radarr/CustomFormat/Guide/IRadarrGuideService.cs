using System.Collections.Generic;

namespace TrashLib.Radarr.CustomFormat.Guide;

public interface IRadarrGuideService
{
    IEnumerable<string> GetCustomFormatJson();
}
