using System.IO.Abstractions;
using Recyclarr.Repo;

namespace Recyclarr.TrashGuide.CustomFormat;

public class CustomFormatGuideService(
    IRepoMetadataBuilder metadataBuilder,
    ICustomFormatLoader cfLoader)
    : ICustomFormatGuideService
{
    private readonly Dictionary<SupportedServices, ICollection<CustomFormatData>> _cache = new();

    private CustomFormatPaths CreatePaths(SupportedServices serviceType)
    {
        var metadata = metadataBuilder.GetMetadata();
        return serviceType switch
        {
            SupportedServices.Radarr => new CustomFormatPaths(
                metadataBuilder.ToDirectoryInfoList(metadata.JsonPaths.Radarr.CustomFormats),
                metadataBuilder.DocsDirectory.SubDirectory("Radarr").File("Radarr-collection-of-custom-formats.md")
            ),
            SupportedServices.Sonarr => new CustomFormatPaths(
                metadataBuilder.ToDirectoryInfoList(metadata.JsonPaths.Sonarr.CustomFormats),
                metadataBuilder.DocsDirectory.SubDirectory("Sonarr").File("sonarr-collection-of-custom-formats.md")
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceType), serviceType, null)
        };
    }

    public ICollection<CustomFormatData> GetCustomFormatData(SupportedServices serviceType)
    {
        if (_cache.TryGetValue(serviceType, out var cfData))
        {
            return cfData;
        }

        var paths = CreatePaths(serviceType);

        return _cache[serviceType] = cfLoader.LoadAllCustomFormatsAtPaths(
            paths.CustomFormatDirectories,
            paths.CollectionOfCustomFormatsMarkdown);
    }
}
