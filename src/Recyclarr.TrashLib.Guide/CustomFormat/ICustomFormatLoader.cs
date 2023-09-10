using System.IO.Abstractions;
using Recyclarr.TrashLib.Models;

namespace Recyclarr.TrashLib.Guide.CustomFormat;

public interface ICustomFormatLoader
{
    ICollection<CustomFormatData> LoadAllCustomFormatsAtPaths(
        IEnumerable<IDirectoryInfo> jsonPaths,
        IFileInfo collectionOfCustomFormats);
}
