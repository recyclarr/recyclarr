namespace Recyclarr.ConfigTemplates;

public class ConfigTemplatesResourceQuery(
    IEnumerable<IConfigTemplatesResourceProvider> templatesProviders
) : IConfigTemplatesResourceQuery
{
    private readonly Lazy<IReadOnlyCollection<TemplatePath>> _templatesCache = new(() =>
        templatesProviders
            .SelectMany(provider => provider.GetTemplates())
            .GroupBy(t => t.Id)
            .Select(group => group.Last()) // Last occurrence wins precedence
            .ToList()
    );

    public IReadOnlyCollection<TemplatePath> GetTemplates() => _templatesCache.Value;
}
