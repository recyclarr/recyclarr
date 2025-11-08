using System.IO.Abstractions;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.TrashGuide;

internal class TrashGuidesDirectoryBasedResourceProvider(
    ISettings<ResourceProviderSettings> settings,
    IFileSystem fileSystem
) : TrashGuidesResourceProvider
{
    protected override IEnumerable<IDirectoryInfo> GetSourceDirectories()
    {
        var localProviders = settings
            .Value.Providers.Where(p => p.Type == "trash-guides" && p is LocalResourceProvider)
            .Cast<LocalResourceProvider>();

        return localProviders
            .Where(provider => fileSystem.Directory.Exists(provider.Path))
            .Select(provider => fileSystem.DirectoryInfo.New(provider.Path));
    }
}
