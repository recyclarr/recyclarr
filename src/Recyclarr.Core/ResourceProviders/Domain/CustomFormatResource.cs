using System.IO.Abstractions;
using System.Text.Json.Serialization;
using Recyclarr.Json;
using Recyclarr.SyncState;
using Recyclarr.TrashGuide.CustomFormat;

namespace Recyclarr.ResourceProviders.Domain;

public record CustomFormatResource : IGuideResource, IServiceResource
{
    [JsonIgnore]
    public IFileInfo? SourceFile { get; init; }

    [JsonPropertyName("trash_id")]
    [JsonNoSerialize]
    public string TrashId { get; init; } = "";

    [JsonPropertyName("trash_scores")]
    [JsonNoSerialize]
    public Dictionary<string, int> TrashScores { get; init; } =
        new(StringComparer.InvariantCultureIgnoreCase);

    [JsonIgnore]
    public int? DefaultScore => TrashScores.TryGetValue("default", out var score) ? score : null;

    public int Id { get; set; }
    public string Name { get; init; } = "";

    [JsonIgnore]
    public MappingKey MappingKey => new(TrashId, Name);
    public bool IncludeCustomFormatWhenRenaming { get; init; }
    public IReadOnlyCollection<CustomFormatSpecificationData> Specifications { get; init; } = [];
}

public record RadarrCustomFormatResource : CustomFormatResource;

public record SonarrCustomFormatResource : CustomFormatResource;
