using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Recyclarr.Repo;
using Recyclarr.Settings.Models;
using Recyclarr.TestLibrary;

namespace Recyclarr.Core.TestLibrary;

public class StubRepoUpdater(MockFileSystem fileSystem) : IRepoUpdater
{
    public Task UpdateRepo(
        IDirectoryInfo repoPath,
        GitRepositorySource repositorySource,
        CancellationToken token
    )
    {
        switch (repoPath.Parent?.Name)
        {
            case "trash-guides":
                SetupTrashGuidesRepo(repoPath);
                break;
            case "config-templates":
                // Kept for future reference
                break;
            default:
                throw new InvalidOperationException("Unknown repository type");
        }

        return Task.CompletedTask;
    }

    private void SetupTrashGuidesRepo(IDirectoryInfo repoPath)
    {
        // Load embedded resources and add to mock filesystem
        fileSystem.AddFileFromEmbeddedResource(
            repoPath.File("metadata.json"),
            GetType(),
            "Data/TrashGuides/metadata.json"
        );

        fileSystem.AddFileFromEmbeddedResource(
            repoPath
                .SubDirectory("docs")
                .SubDirectory("Radarr")
                .File("Radarr-collection-of-custom-formats.md"),
            GetType(),
            "Data/TrashGuides/Radarr-collection-of-custom-formats.md"
        );

        fileSystem.AddFileFromEmbeddedResource(
            repoPath
                .SubDirectory("docs")
                .SubDirectory("Sonarr")
                .File("sonarr-collection-of-custom-formats.md"),
            GetType(),
            "Data/TrashGuides/sonarr-collection-of-custom-formats.md"
        );
    }
}
