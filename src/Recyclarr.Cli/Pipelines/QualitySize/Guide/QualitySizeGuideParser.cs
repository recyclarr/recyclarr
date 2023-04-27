using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Recyclarr.Common;
using Recyclarr.Common.Extensions;

namespace Recyclarr.Cli.Pipelines.QualitySize.Guide;

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

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification =
        "Exceptions not rethrown so we can continue processing other files")]
    private QualitySizeData? ParseQuality(IFileInfo jsonFile)
    {
        var serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        });

        QualitySizeData? quality = null;
        Exception? exception = null;

        using var json = new JsonTextReader(jsonFile.OpenText());
        try
        {
            quality = serializer.Deserialize<QualitySizeData>(json);
        }
        catch (Exception e)
        {
            exception = e;
        }

        if (exception is not null || quality is null)
        {
            _log.Warning(exception, "Failed to parse quality definition JSON file: {Filename}", jsonFile.FullName);
        }

        return quality;
    }
}
