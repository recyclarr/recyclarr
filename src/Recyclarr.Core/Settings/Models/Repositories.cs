namespace Recyclarr.Settings.Models;

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record Repositories
{
    public TrashRepository TrashGuides { get; init; } = new();
    public ConfigTemplateRepository ConfigTemplates { get; init; } = new();
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record TrashRepository : IRepositorySettings
{
    public Uri CloneUrl { get; init; } = new("https://github.com/TRaSH-Guides/Guides.git");
    public string Branch { get; init; } = "master";
    public string? Sha1 { get; init; }
}

[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.WithMembers)]
public record ConfigTemplateRepository : IRepositorySettings
{
    public Uri CloneUrl { get; init; } = new("https://github.com/recyclarr/config-templates.git");
    public string Branch { get; init; } = "master";
    public string? Sha1 { get; init; }
}
