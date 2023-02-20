using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Pipelines.CustomFormat.Guide;

public interface ICustomFormatCategoryParser
{
    ICollection<CustomFormatCategoryItem> Parse(IFileInfo collectionOfCustomFormatsMdFile);
}
