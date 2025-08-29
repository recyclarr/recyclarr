using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Settings.Models;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record ResourceProviderSettings
{
    public IReadOnlyCollection<IUnderlyingResourceProvider> TrashGuides { get; init; } = [];
    public IReadOnlyCollection<IUnderlyingResourceProvider> ConfigTemplates { get; init; } = [];
    public ServiceSpecificResourceProviders CustomFormats { get; init; } = new();
    public ServiceSpecificResourceProviders MediaNaming { get; init; } = new();
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record ServiceSpecificResourceProviders
{
    public IReadOnlyCollection<IUnderlyingResourceProvider> Radarr { get; init; } = [];
    public IReadOnlyCollection<IUnderlyingResourceProvider> Sonarr { get; init; } = [];
}

[SuppressMessage("Design", "CA1040:Avoid empty interfaces")]
public interface IUnderlyingResourceProvider;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record GitRepositorySource : IUnderlyingResourceProvider
{
    public string? Name { get; init; }
    public Uri? CloneUrl { get; init; }
    public string? Reference { get; init; }
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record LocalPathSource : IUnderlyingResourceProvider
{
    public string Path { get; init; } = "";
    public string Service { get; init; } = "";
}
