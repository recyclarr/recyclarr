using System.IO.Abstractions;
using TrashLib.Radarr.Config;

namespace TrashLib.Radarr.CustomFormat.Guide;

public class LocalRepoCustomFormatJsonParser : IRadarrGuideService
{
    private readonly IFileSystem _fileSystem;
    private readonly IResourcePaths _paths;

    public LocalRepoCustomFormatJsonParser(IFileSystem fileSystem, IResourcePaths paths)
    {
        _fileSystem = fileSystem;
        _paths = paths;
    }

    public IEnumerable<string> GetCustomFormatJson()
    {
        var jsonDir = Path.Combine(_paths.RepoPath, "docs/json/radarr");
        var tasks = _fileSystem.Directory.GetFiles(jsonDir, "*.json")
            .Select(f => _fileSystem.File.ReadAllTextAsync(f));

        return Task.WhenAll(tasks).Result;
    }
}
