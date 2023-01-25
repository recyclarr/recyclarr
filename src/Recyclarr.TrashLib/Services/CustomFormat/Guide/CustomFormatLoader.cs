using System.IO.Abstractions;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Newtonsoft.Json;
using Recyclarr.Common;
using Recyclarr.Common.Extensions;
using Recyclarr.TrashLib.Services.CustomFormat.Models;

namespace Recyclarr.TrashLib.Services.CustomFormat.Guide;

public class CustomFormatLoader : ICustomFormatLoader
{
    private readonly ILogger _log;
    private readonly ICustomFormatParser _parser;
    private readonly ICustomFormatCategoryParser _categoryParser;

    public CustomFormatLoader(ILogger log, ICustomFormatParser parser, ICustomFormatCategoryParser categoryParser)
    {
        _log = log;
        _parser = parser;
        _categoryParser = categoryParser;
    }

    public ICollection<CustomFormatData> LoadAllCustomFormatsAtPaths(
        IEnumerable<IDirectoryInfo> jsonPaths,
        IFileInfo collectionOfCustomFormats)
    {
        var categories = _categoryParser.Parse(collectionOfCustomFormats).AsReadOnly();
        var jsonFiles = JsonUtils.GetJsonFilesInDirectories(jsonPaths, _log);
        return jsonFiles.ToObservable()
            .Select(x => Observable.Defer(() => LoadJsonFromFile(x, categories)))
            .Merge(8)
            .NotNull()
            .ToEnumerable()
            .ToList();
    }

    private IObservable<CustomFormatData?> LoadJsonFromFile(IFileInfo file,
        IReadOnlyCollection<CustomFormatCategoryItem> categories)
    {
        return Observable.Using(file.OpenText, x => x.ReadToEndAsync().ToObservable())
            .Select(x =>
            {
                var cf = _parser.ParseCustomFormatData(x, file.Name);
                var matchingCategory = categories.FirstOrDefault(y =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(cf.FileName);
                    return y.CfName.EqualsIgnoreCase(cf.Name) || y.CfAnchor.EqualsIgnoreCase(fileName);
                });
                return cf with {Category = matchingCategory?.CategoryName};
            })
            .Catch((JsonException e) =>
            {
                _log.Warning("Failed to parse JSON file: {File} ({Reason})", file.Name, e.Message);
                return Observable.Empty<CustomFormatData>();
            });
    }
}
