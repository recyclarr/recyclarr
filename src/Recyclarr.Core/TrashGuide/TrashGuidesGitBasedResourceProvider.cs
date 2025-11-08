using System.IO.Abstractions;
using Recyclarr.ResourceProviders.Git;

namespace Recyclarr.TrashGuide;

internal class TrashGuidesGitBasedResourceProvider(
    TrashGuidesRepositoryDefinitionProvider definitionProvider
) : TrashGuidesResourceProvider
{
    protected override IEnumerable<IDirectoryInfo> GetSourceDirectories()
    {
        return definitionProvider.RepositoryDefinitions.Select(repo => repo.Path);
    }
}
