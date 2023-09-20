using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Json;

namespace Recyclarr.TrashLib.Guide.ReleaseProfile;

public class ReleaseProfileGuideParser
{
    private readonly ILogger _log;
    private readonly JsonSerializerOptions _jsonSettings;

    public ReleaseProfileGuideParser(ILogger log)
    {
        _log = log;
        _jsonSettings = new JsonSerializerOptions(GlobalJsonSerializerSettings.Services)
        {
            Converters =
            {
                new CollectionJsonConverter(),
                new TermDataConverter()
            }
        };
    }

    private async Task<ReleaseProfileData?> LoadAndParseFile(IFileInfo file)
    {
        try
        {
            await using var stream = file.OpenRead();
            return await JsonSerializer.DeserializeAsync<ReleaseProfileData>(stream, _jsonSettings);
        }
        catch (JsonException e)
        {
            HandleJsonException(e, file);
        }
        catch (AggregateException ae) when (ae.InnerException is JsonException e)
        {
            HandleJsonException(e, file);
        }

        return null;
    }

    private void HandleJsonException(JsonException exception, IFileInfo file)
    {
        _log.Warning(exception,
            "Failed to parse Sonarr JSON file (This likely indicates a bug that should be " +
            "reported in the TRaSH repo): {File}", file.Name);
    }

    public IEnumerable<ReleaseProfileData> GetReleaseProfileData(IEnumerable<IDirectoryInfo> paths)
    {
        var tasks = JsonUtils.GetJsonFilesInDirectories(paths, _log).Select(LoadAndParseFile);
        var data = Task.WhenAll(tasks).Result
            // Make non-nullable type and filter out null values
            .Choose(x => x is not null ? (true, x) : default);

        var validator = new ReleaseProfileDataValidationFilterer(_log);
        return validator.FilterProfiles(data);
    }
}
