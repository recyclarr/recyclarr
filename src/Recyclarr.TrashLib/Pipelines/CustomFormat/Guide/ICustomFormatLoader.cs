using System.IO.Abstractions;
using Recyclarr.TrashLib.Pipelines.CustomFormat.Models;

namespace Recyclarr.TrashLib.Pipelines.CustomFormat.Guide;

public interface ICustomFormatLoader
{
    ICollection<CustomFormatData> LoadAllCustomFormatsAtPaths(
        IEnumerable<IDirectoryInfo> jsonPaths,
        IFileInfo collectionOfCustomFormats);
}
