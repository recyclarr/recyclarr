namespace Recyclarr.ConfigTemplates;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public record TemplateEntry(string Id, string Template, bool Hidden = false);

public record TemplatesData
{
    public IReadOnlyCollection<TemplateEntry> Radarr { get; init; } = [];
    public IReadOnlyCollection<TemplateEntry> Sonarr { get; init; } = [];
}

public class ConfigTemplatesResourceQuery(
    IEnumerable<IConfigTemplatesResourceProvider> templatesProviders,
    IEnumerable<IConfigIncludesResourceProvider> includesProviders
) : IConfigTemplatesResourceQuery
{
    private IReadOnlyCollection<TemplatePath>? _templateData;
    private IReadOnlyCollection<IncludePath>? _includeData;

    public IReadOnlyCollection<TemplatePath> GetTemplates()
    {
        return _templateData ??= templatesProviders
            .SelectMany(provider => provider.GetTemplates())
            .ToList();
    }

    public IReadOnlyCollection<IncludePath> GetIncludes()
    {
        return _includeData ??= includesProviders
            .SelectMany(provider => provider.GetIncludes())
            .ToList();
    }
}
