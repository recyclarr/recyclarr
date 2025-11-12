using System.IO.Abstractions;

namespace Recyclarr.ResourceProviders.Domain;

public record ConfigIncludeResource
{
    public required string Id { get; init; }
    public required IFileInfo TemplateFile { get; init; }
    public bool Hidden { get; init; }
}

public record RadarrConfigIncludeResource : ConfigIncludeResource;

public record SonarrConfigIncludeResource : ConfigIncludeResource;
