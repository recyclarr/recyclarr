namespace Recyclarr.TrashGuide.MediaNaming;

public record RadarrMediaNamingData
{
    public IReadOnlyDictionary<string, string> Folder { get; init; } =
        new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> File { get; init; } =
        new Dictionary<string, string>();
}
