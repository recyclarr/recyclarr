namespace Recyclarr.Settings.Models;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public abstract record ResourceProvider
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public bool ReplaceDefault { get; init; }
    public string? Service { get; init; }
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record GitResourceProvider : ResourceProvider
{
    public required Uri CloneUrl { get; init; }
    public string? Reference { get; init; }
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record LocalResourceProvider : ResourceProvider
{
    public required string Path { get; init; }
}
