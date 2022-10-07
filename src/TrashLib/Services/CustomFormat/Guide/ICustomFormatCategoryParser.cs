using System.IO.Abstractions;

namespace TrashLib.Services.CustomFormat.Guide;

public interface ICustomFormatCategoryParser
{
    ICollection<CustomFormatCategoryItem> Parse(IFileInfo collectionOfCustomFormatsMdFile);
}
