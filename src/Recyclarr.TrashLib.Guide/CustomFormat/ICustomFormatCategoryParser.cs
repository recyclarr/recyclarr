using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Guide.CustomFormat;

public interface ICustomFormatCategoryParser
{
    ICollection<CustomFormatCategoryItem> Parse(IFileInfo collectionOfCustomFormatsMdFile);
}
