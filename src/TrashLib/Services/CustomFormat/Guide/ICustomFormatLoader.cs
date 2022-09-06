using System.IO.Abstractions;
using TrashLib.Services.CustomFormat.Models;

namespace TrashLib.Services.CustomFormat.Guide;

public interface ICustomFormatLoader
{
    ICollection<CustomFormatData> LoadAllCustomFormatsAtPaths(IEnumerable<IDirectoryInfo> jsonPaths);
}
