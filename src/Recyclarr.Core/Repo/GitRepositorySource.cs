using System.IO.Abstractions;

namespace Recyclarr.Repo;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record GitRepositorySource
{
    [UsedImplicitly]
    public required string Name { get; init; }
    public required Uri CloneUrl { get; init; }
    public string Reference { get; init; } = "master";
    public required IDirectoryInfo Path { get; init; }
    public int Depth { get; init; }
}
