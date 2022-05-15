using System.IO.Abstractions;

namespace TrashLib.Radarr.CustomFormat.Guide;

public class LocalRepoCustomFormatJsonParser : IRadarrGuideService
{
    private readonly IFileSystem _fileSystem;
    private readonly IAppPaths _paths;

    public LocalRepoCustomFormatJsonParser(IFileSystem fileSystem, IAppPaths paths)
    {
        _fileSystem = fileSystem;
        _paths = paths;
    }

    public IEnumerable<string> GetCustomFormatJson()
    {
        var jsonDir = Path.Combine(_paths.RepoDirectory, "docs/json/radarr");
        var tasks = _fileSystem.Directory.GetFiles(jsonDir, "*.json")
            .Select(f => _fileSystem.File.ReadAllTextAsync(f));

        return Task.WhenAll(tasks).Result;
    }
}
