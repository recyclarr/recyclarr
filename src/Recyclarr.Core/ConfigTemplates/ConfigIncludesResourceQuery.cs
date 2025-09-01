namespace Recyclarr.ConfigTemplates;

public class ConfigIncludesResourceQuery(
    IEnumerable<IConfigIncludesResourceProvider> includesProviders
) : IConfigIncludesResourceQuery
{
    private readonly Lazy<IReadOnlyCollection<TemplatePath>> _includesCache = new(() =>
        includesProviders.SelectMany(provider => provider.GetIncludes()).ToList()
    );

    public IReadOnlyCollection<TemplatePath> GetIncludes() => _includesCache.Value;
}
