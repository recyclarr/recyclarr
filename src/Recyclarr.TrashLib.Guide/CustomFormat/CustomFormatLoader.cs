using System.IO.Abstractions;
using System.Reactive.Linq;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Json;
using Recyclarr.TrashLib.Models;

namespace Recyclarr.TrashLib.Guide.CustomFormat;

public class CustomFormatLoader : ICustomFormatLoader
{
    private readonly BulkJsonLoader _loader;
    private readonly ICustomFormatCategoryParser _categoryParser;

    public CustomFormatLoader(BulkJsonLoader loader, ICustomFormatCategoryParser categoryParser)
    {
        _loader = loader;
        _categoryParser = categoryParser;
    }

    public ICollection<CustomFormatData> LoadAllCustomFormatsAtPaths(
        IEnumerable<IDirectoryInfo> jsonPaths,
        IFileInfo collectionOfCustomFormats)
    {
        var categories = _categoryParser.Parse(collectionOfCustomFormats).AsReadOnly();
        return _loader.LoadAllFilesAtPaths<CustomFormatData>(jsonPaths, x => x.Select(cf =>
        {
            var matchingCategory = categories.FirstOrDefault(
                y => y.CfName.EqualsIgnoreCase(cf.Obj.Name) || y.CfAnchor.EqualsIgnoreCase(cf.File.Name));

            return cf.Obj with
            {
                Category = matchingCategory?.CategoryName
            };
        }));
    }
}
