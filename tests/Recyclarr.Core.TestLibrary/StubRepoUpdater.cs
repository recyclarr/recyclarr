using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using Recyclarr.Repo;
using Recyclarr.Settings;

namespace Recyclarr.Core.TestLibrary;

public class StubRepoUpdater : IRepoUpdater
{
    private readonly MockFileSystem _fileSystem;

    public StubRepoUpdater(MockFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public Task UpdateRepo(IDirectoryInfo repoPath, IRepositorySettings repoSettings, CancellationToken token)
    {
        var repoType = DetectRepoTypeFromPath(repoPath);
        
        switch (repoType)
        {
            case "trash-guides":
                SetupTrashGuidesRepo(repoPath);
                break;
            case "config-templates":
                SetupConfigTemplatesRepo(repoPath);
                break;
            default:
                // Default to trash-guides structure for custom repos
                SetupTrashGuidesRepo(repoPath);
                break;
        }

        return Task.CompletedTask;
    }

    private static string DetectRepoTypeFromPath(IDirectoryInfo path)
    {
        // Path pattern: repositories/{type}/{name}
        var parentName = path.Parent?.Name;
        return parentName switch
        {
            "trash-guides" => "trash-guides",
            "config-templates" => "config-templates",
            _ => "trash-guides" // Default for custom repos
        };
    }

    private void SetupTrashGuidesRepo(IDirectoryInfo repoPath)
    {
        // Load embedded resources and add to mock filesystem
        _fileSystem.AddFile(
            repoPath.File("metadata.json"), 
            new MockFileData(LoadEmbeddedResource("TrashGuides.metadata.json"))
        );
        
        _fileSystem.AddFile(
            repoPath.SubDirectory("docs").SubDirectory("Radarr").File("Radarr-collection-of-custom-formats.md"),
            new MockFileData(LoadEmbeddedResource("TrashGuides.Radarr-collection-of-custom-formats.md"))
        );
        
        _fileSystem.AddFile(
            repoPath.SubDirectory("docs").SubDirectory("Sonarr").File("sonarr-collection-of-custom-formats.md"),
            new MockFileData(LoadEmbeddedResource("TrashGuides.sonarr-collection-of-custom-formats.md"))
        );
    }

    private void SetupConfigTemplatesRepo(IDirectoryInfo repoPath)
    {
        // For now, just create minimal structure for config templates
        _fileSystem.AddFile(
            repoPath.File("templates.json"),
            new MockFileData("{\"Radarr\": [], \"Sonarr\": []}")
        );
        
        _fileSystem.AddFile(
            repoPath.File("includes.json"),
            new MockFileData("{\"Radarr\": [], \"Sonarr\": []}")
        );
    }

    private static string LoadEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var fullResourceName = $"Recyclarr.Core.TestLibrary.Data.{resourceName}";
        
        using var stream = assembly.GetManifestResourceStream(fullResourceName);
        if (stream == null)
        {
            throw new FileNotFoundException($"Embedded resource not found: {fullResourceName}");
        }
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}