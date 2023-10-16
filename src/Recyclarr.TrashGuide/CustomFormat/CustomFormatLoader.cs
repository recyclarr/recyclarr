using System.IO.Abstractions;
using System.Reactive.Linq;
using Recyclarr.Common.Extensions;
using Recyclarr.Json.Loading;

namespace Recyclarr.TrashGuide.CustomFormat;

public class CustomFormatLoader : ICustomFormatLoader
{
    private readonly ServiceJsonLoader _loader;
    private readonly ICustomFormatCategoryParser _categoryParser;
    private readonly IFileSystem _fs;

    public CustomFormatLoader(ServiceJsonLoader loader, ICustomFormatCategoryParser categoryParser, IFileSystem fs)
    {
        _loader = loader;
        _categoryParser = categoryParser;
        _fs = fs;
    }

    public ICollection<CustomFormatData> LoadAllCustomFormatsAtPaths(
        IEnumerable<IDirectoryInfo> jsonPaths,
        IFileInfo collectionOfCustomFormats)
    {
        var categories = _categoryParser.Parse(collectionOfCustomFormats).AsReadOnly();
        return _loader.LoadAllFilesAtPaths<CustomFormatData>(jsonPaths, x => x.Select(cf =>
        {
            var matchingCategory = categories.FirstOrDefault(y =>
                y.CfName.EqualsIgnoreCase(cf.Obj.Name) ||
                y.CfAnchor.EqualsIgnoreCase(_fs.Path.GetFileNameWithoutExtension(cf.File.Name)));

            return cf.Obj with
            {
                Category = matchingCategory?.CategoryName
            };
        }));
    }
}
