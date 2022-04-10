using System.IO.Abstractions;
using Common.FluentValidation;
using MoreLinq;
using Newtonsoft.Json;
using TrashLib.Radarr.Config;

namespace TrashLib.Sonarr.ReleaseProfile.Guide;

public class LocalRepoReleaseProfileJsonParser : ISonarrGuideService
{
    private readonly IFileSystem _fileSystem;
    private readonly IResourcePaths _paths;

    public LocalRepoReleaseProfileJsonParser(IFileSystem fileSystem, IResourcePaths paths)
    {
        _fileSystem = fileSystem;
        _paths = paths;
    }

    public IReadOnlyCollection<ReleaseProfileData> GetReleaseProfileData()
    {
        var converter = new TermDataConverter();
        var jsonDir = Path.Combine(_paths.RepoPath, "docs/json/sonarr");
        var tasks = _fileSystem.Directory.GetFiles(jsonDir, "*.json")
            .Select(async f =>
            {
                var json = await _fileSystem.File.ReadAllTextAsync(f);
                return JsonConvert.DeserializeObject<ReleaseProfileData>(json, converter);
            });

        return Task.WhenAll(tasks).Result
            .Choose(x => x is not null ? (true, x) : default) // Make non-nullable type
            .IsValid(new ReleaseProfileDataValidator())
            .ToList();
    }
}
