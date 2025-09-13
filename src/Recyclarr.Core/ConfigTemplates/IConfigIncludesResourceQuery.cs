namespace Recyclarr.ConfigTemplates;

public interface IConfigIncludesResourceQuery
{
    IReadOnlyCollection<TemplatePath> GetIncludes();
}
