using TrashLib.Radarr.CustomFormat.Models;

namespace TrashLib.Radarr.CustomFormat.Processors;

public interface ICustomFormatLookup
{
    Dictionary<string, List<CustomFormatData>> MapAllCustomFormats();
}
