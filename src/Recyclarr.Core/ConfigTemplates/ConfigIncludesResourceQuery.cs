namespace Recyclarr.ConfigTemplates;

public class ConfigIncludesResourceQuery(
    IEnumerable<IConfigIncludesResourceProvider> includesProviders
) : IConfigIncludesResourceQuery
{
    private readonly Lazy<IReadOnlyCollection<TemplatePath>> _includesCache = new(() =>
        includesProviders
            .SelectMany(provider => provider.GetIncludes())
            .DistinctBy(t => t.Id) // First occurrence wins precedence
            .ToList()
    );

    public IReadOnlyCollection<TemplatePath> GetIncludes() => _includesCache.Value;
}
