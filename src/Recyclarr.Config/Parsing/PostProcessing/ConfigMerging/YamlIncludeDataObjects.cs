using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;

[SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification =
    "Used for type-discriminating node deserializer")]
public interface IYamlInclude
{
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record ConfigYamlInclude : IYamlInclude
{
    public string? Config { get; init; }
}

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public record TemplateYamlInclude : IYamlInclude
{
    public string? Template { get; init; }
}
