using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Common.Extensions;
using Recyclarr.Json;

namespace Recyclarr.TrashGuide.QualitySize;

public class QualitySizeGuideParser(ILogger log)
{
    public IReadOnlyList<QualitySizeData> GetQualities(IEnumerable<IDirectoryInfo> jsonDirectories)
    {
        return JsonUtils.GetJsonFilesInDirectories(jsonDirectories, log)
            .Select(ParseQuality)
            .NotNull()
            .ToList();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification =
        "Exceptions not rethrown so we can continue processing other files")]
    private QualitySizeData? ParseQuality(IFileInfo jsonFile)
    {
        QualitySizeData? quality = null;
        Exception? exception = null;

        try
        {
            using var stream = jsonFile.OpenRead();
            quality = JsonSerializer.Deserialize<QualitySizeData>(stream, GlobalJsonSerializerSettings.Guide);
        }
        catch (Exception e)
        {
            exception = e;
        }

        if (exception is not null || quality is null)
        {
            log.Warning(exception, "Failed to parse quality definition JSON file: {Filename}", jsonFile.FullName);
        }

        return quality;
    }
}
