using System.IO.Abstractions;
using Recyclarr.ResourceProviders.Git;

namespace Recyclarr.ConfigTemplates;

internal class ConfigTemplatesGitBasedResourceProvider(IGitRepositoryService gitRepositoryService)
    : ConfigTemplatesResourceProvider
{
    protected override IEnumerable<IDirectoryInfo> GetSourceDirectories()
    {
        return gitRepositoryService.GetRepositoriesOfType("config-templates");
    }
}
