using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Repo;

public interface IRepoPaths
{
    IReadOnlyCollection<IDirectoryInfo> RadarrCustomFormatPaths { get; }
    IReadOnlyCollection<IDirectoryInfo> SonarrReleaseProfilePaths { get; }
    IReadOnlyCollection<IDirectoryInfo> SonarrQualityPaths { get; }
    IReadOnlyCollection<IDirectoryInfo> RadarrQualityPaths { get; }
    IReadOnlyCollection<IDirectoryInfo> SonarrCustomFormatPaths { get; }
    IFileInfo RadarrCollectionOfCustomFormats { get; }
    IFileInfo SonarrCollectionOfCustomFormats { get; }
}
