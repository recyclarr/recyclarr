using System.IO.Abstractions;

namespace TrashLib.Repo;

public record RepoPaths(
    IReadOnlyCollection<IDirectoryInfo> RadarrCustomFormatPaths,
    IReadOnlyCollection<IDirectoryInfo> SonarrReleaseProfilePaths,
    IReadOnlyCollection<IDirectoryInfo> RadarrQualityPaths,
    IReadOnlyCollection<IDirectoryInfo> SonarrQualityPaths,
    IReadOnlyCollection<IDirectoryInfo> SonarrCustomFormatPaths,
    IFileInfo RadarrCollectionOfCustomFormats,
    IFileInfo SonarrCollectionOfCustomFormats
) : IRepoPaths;
