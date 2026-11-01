namespace Recyclarr.ResourceProviders.Domain;

public record ConflictingCustomFormat
{
    public string Name { get; init; } = "";
}

/// <summary>
/// The guide's <c>conflicts.json</c>: each entry maps the trash_ids of custom formats that must not
/// be enabled together.
/// </summary>
public record ConflictsResource
{
    public IReadOnlyCollection<
        IReadOnlyDictionary<string, ConflictingCustomFormat>
    > CustomFormats { get; init; } = [];
}

public record RadarrConflictsResource : ConflictsResource;

public record SonarrConflictsResource : ConflictsResource;
