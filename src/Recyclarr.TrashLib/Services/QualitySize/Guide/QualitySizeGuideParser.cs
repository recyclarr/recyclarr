using System.IO.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Recyclarr.Common;
using Recyclarr.Common.Extensions;

namespace Recyclarr.TrashLib.Services.QualitySize.Guide;

public class QualitySizeGuideParser
{
    private readonly ILogger _log;

    public QualitySizeGuideParser(ILogger log)
    {
        _log = log;
    }

    public IReadOnlyList<QualitySizeData> GetQualities(IEnumerable<IDirectoryInfo> jsonDirectories)
    {
        return JsonUtils.GetJsonFilesInDirectories(jsonDirectories, _log)
            .Select(ParseQuality)
            .NotNull()
            .ToList();
    }

    private QualitySizeData? ParseQuality(IFileInfo jsonFile)
    {
        var serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        });

        using var json = new JsonTextReader(jsonFile.OpenText());
        var quality = serializer.Deserialize<QualitySizeData>(json);
        if (quality is null)
        {
            _log.Debug("Failed to parse quality definition JSON file: {Filename}", jsonFile.FullName);
        }

        return quality;
    }
}
