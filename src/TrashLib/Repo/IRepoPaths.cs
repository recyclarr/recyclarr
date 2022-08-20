using System.IO.Abstractions;

namespace TrashLib.Repo;

public interface IRepoPaths
{
    IReadOnlyCollection<IDirectoryInfo> RadarrCustomFormatPaths { get; }
    IReadOnlyCollection<IDirectoryInfo> SonarrReleaseProfilePaths { get; }
}
