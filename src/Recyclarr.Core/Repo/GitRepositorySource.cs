using System.IO.Abstractions;
using Recyclarr.Common;

namespace Recyclarr.Repo;

public record GitRepositorySource
{
    public required string Name { get; init; }
    public required Uri CloneUrl { get; init; }
    public required IReadOnlyList<string> References { get; init; }
    public required IDirectoryInfo Path { get; init; }

    // Threshold for .git directory size; Bytes == 0 disables maintenance.
    public DataSize CacheLimit { get; init; }
}
