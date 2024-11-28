using System.IO.Abstractions;

namespace Recyclarr.TrashGuide.CustomFormat;

public interface ICustomFormatLoader
{
    ICollection<CustomFormatData> LoadAllCustomFormatsAtPaths(
        IEnumerable<IDirectoryInfo> jsonPaths,
        IFileInfo collectionOfCustomFormats
    );
}
