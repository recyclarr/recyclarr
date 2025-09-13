using Recyclarr.ResourceProviders;

namespace Recyclarr.ConfigTemplates;

public interface IConfigTemplatesResourceProvider : IResourceProvider
{
    IReadOnlyCollection<TemplatePath> GetTemplates();
}
