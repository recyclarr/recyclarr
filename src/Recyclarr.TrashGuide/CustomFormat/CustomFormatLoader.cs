using System.IO.Abstractions;
using System.Reactive.Linq;
using Recyclarr.Common.Extensions;
using Recyclarr.Json.Loading;

namespace Recyclarr.TrashGuide.CustomFormat;

public class CustomFormatLoader(ServiceJsonLoader loader, ICustomFormatCategoryParser categoryParser, IFileSystem fs)
    : ICustomFormatLoader
{
    public ICollection<CustomFormatData> LoadAllCustomFormatsAtPaths(
        IEnumerable<IDirectoryInfo> jsonPaths,
        IFileInfo collectionOfCustomFormats)
    {
        var categories = categoryParser.Parse(collectionOfCustomFormats).AsReadOnly();
        return loader.LoadAllFilesAtPaths<CustomFormatData>(jsonPaths, x => x.Select(cf =>
        {
            var matchingCategory = categories.FirstOrDefault(y =>
                y.CfName.EqualsIgnoreCase(cf.Obj.Name) ||
                y.CfAnchor.EqualsIgnoreCase(fs.Path.GetFileNameWithoutExtension(cf.File.Name)));

            return cf.Obj with
            {
                Category = matchingCategory?.CategoryName
            };
        }));
    }
}
