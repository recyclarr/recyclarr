using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Services.CustomFormat.Guide;

public interface ICustomFormatCategoryParser
{
    ICollection<CustomFormatCategoryItem> Parse(IFileInfo collectionOfCustomFormatsMdFile);
}
