using System.IO.Abstractions;

namespace TrashLib.Repo;

public record RepoPaths(
    IReadOnlyCollection<IDirectoryInfo> RadarrCustomFormatPaths,
    IReadOnlyCollection<IDirectoryInfo> SonarrReleaseProfilePaths
) : IRepoPaths;
