namespace Recyclarr.ConfigTemplates;

public class ConfigIncludesResourceQuery(
    IEnumerable<IConfigIncludesResourceProvider> includesProviders
) : IConfigIncludesResourceQuery
{
    private readonly Lazy<IReadOnlyCollection<TemplatePath>> _includesCache = new(() =>
        includesProviders
            .SelectMany(provider => provider.GetIncludes())
            .GroupBy(t => t.Id)
            .Select(group => group.Last()) // Last occurrence wins precedence
            .ToList()
    );

    public IReadOnlyCollection<TemplatePath> GetIncludes() => _includesCache.Value;
}
