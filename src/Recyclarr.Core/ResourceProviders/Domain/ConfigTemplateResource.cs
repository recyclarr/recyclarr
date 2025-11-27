using System.IO.Abstractions;

namespace Recyclarr.ResourceProviders.Domain;

public record ConfigTemplateResource
{
    public required string Id { get; init; }
    public required IFileInfo TemplateFile { get; init; }
    public bool Hidden { get; init; }
}

public record RadarrConfigTemplateResource : ConfigTemplateResource;

public record SonarrConfigTemplateResource : ConfigTemplateResource;
