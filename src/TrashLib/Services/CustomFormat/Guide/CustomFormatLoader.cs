using System.IO.Abstractions;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Common.Extensions;
using Newtonsoft.Json;
using Serilog;
using TrashLib.Services.CustomFormat.Models;

namespace TrashLib.Services.CustomFormat.Guide;

public class CustomFormatLoader : ICustomFormatLoader
{
    private readonly ILogger _log;
    private readonly ICustomFormatParser _parser;

    public CustomFormatLoader(ILogger log, ICustomFormatParser parser)
    {
        _log = log;
        _parser = parser;
    }

    public ICollection<CustomFormatData> LoadAllCustomFormatsAtPaths(IEnumerable<IDirectoryInfo> jsonPaths)
    {
        var jsonFiles = jsonPaths.SelectMany(x => x.GetFiles("*.json"));
        return jsonFiles.ToObservable()
            .Select(x => Observable.Defer(() => LoadJsonFromFile(x)))
            .Merge(8)
            .NotNull()
            .ToEnumerable()
            .ToList();
    }

    private IObservable<CustomFormatData?> LoadJsonFromFile(IFileInfo file)
    {
        return Observable.Using(file.OpenText, x => x.ReadToEndAsync().ToObservable())
            .Do(_ => _log.Debug("Parsing CF Json: {Name}", file.Name))
            .Select(_parser.ParseCustomFormatData)
            .Catch((JsonException e) =>
            {
                _log.Warning("Failed to parse JSON file: {File} ({Reason})", file.Name, e.Message);
                return Observable.Empty<CustomFormatData>();
            });
    }
}
