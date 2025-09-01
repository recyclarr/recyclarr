namespace Recyclarr.ConfigTemplates;

public class ConfigTemplatesResourceQuery(
    IEnumerable<IConfigTemplatesResourceProvider> templatesProviders
) : IConfigTemplatesResourceQuery
{
    private readonly Lazy<IReadOnlyCollection<TemplatePath>> _templatesCache = new(() =>
        templatesProviders.SelectMany(provider => provider.GetTemplates()).ToList()
    );

    public IReadOnlyCollection<TemplatePath> GetTemplates() => _templatesCache.Value;
}
