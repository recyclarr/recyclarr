using Recyclarr.ResourceProviders;

namespace Recyclarr.ConfigTemplates;

public interface IConfigIncludesResourceProvider : IResourceProvider
{
    IReadOnlyCollection<TemplatePath> GetIncludes();
}
