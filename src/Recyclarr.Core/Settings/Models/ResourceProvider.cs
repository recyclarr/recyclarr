using Recyclarr.Common;

namespace Recyclarr.Settings.Models;

public abstract record ResourceProvider
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public bool ReplaceDefault { get; init; }
    public string? Service { get; init; }
}

public record GitResourceProvider : ResourceProvider
{
    public required Uri CloneUrl { get; init; }
    public string? Reference { get; init; }
    public DataSize CacheLimit { get; init; } = DataSize.Default;
}

public record LocalResourceProvider : ResourceProvider
{
    public required string Path { get; init; }
}
