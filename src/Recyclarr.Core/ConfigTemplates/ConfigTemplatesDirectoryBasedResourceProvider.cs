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
        var localSources = settings.Value.ConfigTemplates.OfType<LocalPathSource>();

        return localSources
            .Where(source => fileSystem.Directory.Exists(source.Path))
            .Select(source => fileSystem.DirectoryInfo.New(source.Path));
    }
}
