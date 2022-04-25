using System.IO.Abstractions;
using Common.Extensions;
using Common.FluentValidation;
using MoreLinq;
using Newtonsoft.Json;
using TrashLib.Radarr.Config;

namespace TrashLib.Sonarr.ReleaseProfile.Guide;

public class LocalRepoReleaseProfileJsonParser : ISonarrGuideService
{
    private readonly IFileSystem _fileSystem;
    private readonly IResourcePaths _paths;
    private readonly Lazy<IEnumerable<ReleaseProfileData>> _data;

    public LocalRepoReleaseProfileJsonParser(IFileSystem fileSystem, IResourcePaths paths)
    {
        _fileSystem = fileSystem;
        _paths = paths;
        _data = new Lazy<IEnumerable<ReleaseProfileData>>(GetReleaseProfileDataImpl);
    }

    private IEnumerable<ReleaseProfileData> GetReleaseProfileDataImpl()
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
            .Choose(x => x is not null ? (true, x) : default); // Make non-nullable type
    }

    public ReleaseProfileData? GetUnfilteredProfileById(string trashId)
    {
        return _data.Value.FirstOrDefault(x => x.TrashId.EqualsIgnoreCase(trashId));
    }

    public IReadOnlyCollection<ReleaseProfileData> GetReleaseProfileData()
    {
        return _data.Value
            .IsValid(new ReleaseProfileDataValidator())
            .ToList();
    }
}
