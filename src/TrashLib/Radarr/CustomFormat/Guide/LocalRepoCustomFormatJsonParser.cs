using System.IO.Abstractions;
using TrashLib.Repo;

namespace TrashLib.Radarr.CustomFormat.Guide;

internal class LocalRepoCustomFormatJsonParser : IRadarrGuideService
{
    private readonly IFileSystem _fileSystem;
    private readonly IRepoUpdater _repoUpdater;

    public LocalRepoCustomFormatJsonParser(IFileSystem fileSystem, IRepoUpdater repoUpdater)
    {
        _fileSystem = fileSystem;
        _repoUpdater = repoUpdater;
    }

    public IEnumerable<string> GetCustomFormatJson()
    {
        _repoUpdater.UpdateRepo();

        var jsonDir = Path.Combine(_repoUpdater.RepoPath, "docs/json/radarr");
        var tasks = _fileSystem.Directory.GetFiles(jsonDir, "*.json")
            .Select(async f => await _fileSystem.File.ReadAllTextAsync(f));

        return Task.WhenAll(tasks).Result;
    }
}
