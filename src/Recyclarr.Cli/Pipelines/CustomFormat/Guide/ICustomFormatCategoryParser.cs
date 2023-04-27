using System.IO.Abstractions;

namespace Recyclarr.Cli.Pipelines.CustomFormat.Guide;

public interface ICustomFormatCategoryParser
{
    ICollection<CustomFormatCategoryItem> Parse(IFileInfo collectionOfCustomFormatsMdFile);
}
