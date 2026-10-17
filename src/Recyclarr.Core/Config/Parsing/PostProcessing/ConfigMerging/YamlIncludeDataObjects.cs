using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

[SuppressMessage(
    "Design",
    "CA1040:Avoid empty interfaces",
    Justification = "Used for type-discriminating node deserializer"
)]
public interface IYamlInclude;

public record ConfigYamlInclude : IYamlInclude
{
    public string? Config { get; init; }
}

public record TemplateYamlInclude : IYamlInclude
{
    public string? Template { get; init; }
}
