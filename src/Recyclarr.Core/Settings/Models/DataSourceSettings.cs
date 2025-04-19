using System.Diagnostics.CodeAnalysis;

namespace Recyclarr.Settings.Models;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record DataSourceSettings
{
    public IReadOnlyCollection<IUnderlyingDataSource> TrashGuides { get; init; } = [];
    public IReadOnlyCollection<IUnderlyingDataSource> ConfigTemplates { get; init; } = [];
}

[SuppressMessage("Design", "CA1040:Avoid empty interfaces")]
public interface IUnderlyingDataSource;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record GitRepositorySource : IUnderlyingDataSource
{
    public string? Name { get; init; }
    public Uri? CloneUrl { get; init; }
    public string? Reference { get; init; }
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record LocalPathSource : IUnderlyingDataSource
{
    public string Path { get; init; } = "";
    public string Service { get; init; } = "";
}
