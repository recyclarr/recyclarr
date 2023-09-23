using System.IO.Abstractions;

namespace Recyclarr.TrashGuide.CustomFormat;

public interface ICustomFormatCategoryParser
{
    ICollection<CustomFormatCategoryItem> Parse(IFileInfo collectionOfCustomFormatsMdFile);
}
