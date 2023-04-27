using System.IO.Abstractions;
using Recyclarr.TrashLib.Models;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Guide;

public interface ICustomFormatLoader
{
    ICollection<CustomFormatData> LoadAllCustomFormatsAtPaths(
        IEnumerable<IDirectoryInfo> jsonPaths,
        IFileInfo collectionOfCustomFormats);
}
