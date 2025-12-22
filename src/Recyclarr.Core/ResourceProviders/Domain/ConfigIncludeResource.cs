using System.IO.Abstractions;

namespace Recyclarr.ResourceProviders.Domain;

public record ConfigIncludeResource
{
    public string Id { get; init; } = "";
    public IFileInfo TemplateFile { get; init; } = null!;
    public bool Hidden { get; init; }
}

public record RadarrConfigIncludeResource : ConfigIncludeResource;

public record SonarrConfigIncludeResource : ConfigIncludeResource;
