namespace Recyclarr.Config.Parsing;

public record RadarrMovieNamingConfigYaml
{
    public bool? Rename { get; init; }
    public string? Standard { get; init; }
}

public record RadarrMediaNamingConfigYaml
{
    public string? Folder { get; init; }
    public RadarrMovieNamingConfigYaml? Movie { get; init; }
}

public record RadarrConfigYaml : ServiceConfigYaml
{
    public RadarrMediaNamingConfigYaml? MediaNaming { get; init; }
}
