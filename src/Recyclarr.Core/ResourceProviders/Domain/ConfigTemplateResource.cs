using System.IO.Abstractions;

namespace Recyclarr.ResourceProviders.Domain;

public record ConfigTemplateResource
{
    public string Id { get; init; } = "";
    public IFileInfo TemplateFile { get; init; } = null!;
    public bool Hidden { get; init; }
}

public record RadarrConfigTemplateResource : ConfigTemplateResource;

public record SonarrConfigTemplateResource : ConfigTemplateResource;
