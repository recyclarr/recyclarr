using System.IO.Abstractions;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Repo;
using Recyclarr.TrashLib.Services.CustomFormat.Models;

namespace Recyclarr.TrashLib.Services.CustomFormat.Guide;

public class CustomFormatGuideService : ICustomFormatGuideService
{
    private readonly IRepoMetadataBuilder _metadataBuilder;
    private readonly ICustomFormatLoader _cfLoader;

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
        var paths = CreatePaths(serviceType);

        return _cfLoader.LoadAllCustomFormatsAtPaths(
            paths.CustomFormatDirectories,
            paths.CollectionOfCustomFormatsMarkdown);
    }
}
