using System.IO.Abstractions;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.ConfigTemplates;

internal class ConfigTemplatesDirectoryBasedResourceProvider(
    ISettings<ResourceProviderSettings> settings,
    IFileSystem fileSystem
) : ConfigTemplatesResourceProvider
{
    protected override IEnumerable<IDirectoryInfo> GetSourceDirectories()
    {
        var localProviders = settings
            .Value.Providers.Where(p => p.Type == "config-templates" && p is LocalResourceProvider)
            .Cast<LocalResourceProvider>();

        return localProviders
            .Where(provider => fileSystem.Directory.Exists(provider.Path))
            .Select(provider => fileSystem.DirectoryInfo.New(provider.Path));
    }
}
