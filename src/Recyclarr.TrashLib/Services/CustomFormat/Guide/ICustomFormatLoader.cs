using System.IO.Abstractions;
using Recyclarr.TrashLib.Services.CustomFormat.Models;

namespace Recyclarr.TrashLib.Services.CustomFormat.Guide;

public interface ICustomFormatLoader
{
    ICollection<CustomFormatData> LoadAllCustomFormatsAtPaths(IEnumerable<IDirectoryInfo> jsonPaths,
        IFileInfo collectionOfCustomFormats);
}
