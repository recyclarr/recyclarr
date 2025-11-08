using System.IO.Abstractions;
using Recyclarr.ResourceProviders.Git;

namespace Recyclarr.ConfigTemplates;

internal class ConfigTemplatesGitBasedResourceProvider(
    ConfigTemplatesRepositoryDefinitionProvider definitionProvider
) : ConfigTemplatesResourceProvider
{
    protected override IEnumerable<IDirectoryInfo> GetSourceDirectories()
    {
        return definitionProvider.RepositoryDefinitions.Select(repo => repo.Path);
    }
}
