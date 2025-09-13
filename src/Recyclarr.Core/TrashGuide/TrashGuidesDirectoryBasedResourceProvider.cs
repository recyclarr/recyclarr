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
        var localSources = settings.Value.TrashGuides.OfType<LocalPathSource>();

        return localSources
            .Where(source => fileSystem.Directory.Exists(source.Path))
            .Select(source => fileSystem.DirectoryInfo.New(source.Path));
    }
}
