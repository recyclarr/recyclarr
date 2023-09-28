using JetBrains.Annotations;

namespace Recyclarr.Config.Parsing;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record RadarrMovieNamingConfigYaml
{
    public bool? Rename { get; init; }
    public string? Standard { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record RadarrMediaNamingConfigYaml
{
    public string? Folder { get; init; }
    public RadarrMovieNamingConfigYaml? Movie { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record RadarrConfigYaml : ServiceConfigYaml
{
    public RadarrMediaNamingConfigYaml? MediaNaming { get; init; }
}
