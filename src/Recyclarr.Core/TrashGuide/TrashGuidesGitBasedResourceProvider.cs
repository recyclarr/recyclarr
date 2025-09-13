using System.IO.Abstractions;
using Recyclarr.ResourceProviders.Git;

namespace Recyclarr.TrashGuide;

internal class TrashGuidesGitBasedResourceProvider(IGitRepositoryService gitRepositoryService)
    : TrashGuidesResourceProvider
{
    protected override IEnumerable<IDirectoryInfo> GetSourceDirectories()
    {
        return gitRepositoryService.GetRepositoriesOfType("trash-guides");
    }
}
