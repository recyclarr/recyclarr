using System.IO.Abstractions;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Models;
using Recyclarr.TrashLib.Repo;

namespace Recyclarr.TrashLib.Guide.CustomFormat;

public class CustomFormatGuideService : ICustomFormatGuideService
{
    private readonly IRepoMetadataBuilder _metadataBuilder;
    private readonly ICustomFormatLoader _cfLoader;
    private readonly Dictionary<SupportedServices, ICollection<CustomFormatData>> _cache = new();

    public CustomFormatGuideService(
        IRepoMetadataBuilder metadataBuilder,
        ICustomFormatLoader cfLoader)
    {
        _metadataBuilder = metadataBuilder;
        _cfLoader = cfLoader;
    }

    private CustomFormatPaths CreatePaths(SupportedServices serviceType)
    {
        var metadata = _metadataBuilder.GetMetadata();
        return serviceType switch
        {
            SupportedServices.Radarr => new CustomFormatPaths(
                _metadataBuilder.ToDirectoryInfoList(metadata.JsonPaths.Radarr.CustomFormats),
                _metadataBuilder.DocsDirectory.SubDirectory("Radarr").File("Radarr-collection-of-custom-formats.md")
            ),
            SupportedServices.Sonarr => new CustomFormatPaths(
                _metadataBuilder.ToDirectoryInfoList(metadata.JsonPaths.Sonarr.CustomFormats),
                _metadataBuilder.DocsDirectory.SubDirectory("Sonarr").File("sonarr-collection-of-custom-formats.md")
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

        return _cache[serviceType] = _cfLoader.LoadAllCustomFormatsAtPaths(
            paths.CustomFormatDirectories,
            paths.CollectionOfCustomFormatsMarkdown);
    }
}
