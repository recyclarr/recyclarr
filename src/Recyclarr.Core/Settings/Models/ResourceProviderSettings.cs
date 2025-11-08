namespace Recyclarr.Settings.Models;

public record ResourceProviderSettings
{
    public IReadOnlyCollection<ResourceProvider> Providers { get; init; } = [];
}

public abstract record ResourceProvider
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public bool ReplaceDefault { get; init; }
}

public record GitResourceProvider : ResourceProvider
{
    public required Uri CloneUrl { get; init; }
    public string Reference { get; init; } = "master";
}

public record LocalResourceProvider : ResourceProvider
{
    public required string Path { get; init; }
    public string? Service { get; init; }
}
