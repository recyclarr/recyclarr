using System.IO.Abstractions;

namespace Recyclarr.Repo;

public record GitRepositorySource
{
    public required string Name { get; init; }
    public required Uri CloneUrl { get; init; }
    public required IReadOnlyList<string> References { get; init; }
    public required IDirectoryInfo Path { get; init; }
}
