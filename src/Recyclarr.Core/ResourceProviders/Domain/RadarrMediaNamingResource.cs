namespace Recyclarr.ResourceProviders.Domain;

public record RadarrMediaNamingResource
{
    public IReadOnlyDictionary<string, string> Folder { get; init; } =
        new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> File { get; init; } =
        new Dictionary<string, string>();
}
