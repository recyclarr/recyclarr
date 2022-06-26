using System.Collections.ObjectModel;
using TrashLib.Radarr.CustomFormat.Models;

namespace TrashLib.Radarr.CustomFormat.Guide;

public interface ICustomFormatGroupParser
{
    IDictionary<string, ReadOnlyCollection<CustomFormatGroupItem>> Parse();
}
