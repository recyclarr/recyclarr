using System.IO.Abstractions;
using TrashLib.Startup;

namespace TrashLib.Repo;

public class RepoPathsFactory : IRepoPathsFactory
{
    private readonly IAppPaths _paths;
    private readonly IFileSystem _fs;
    private readonly Lazy<RepoMetadata> _metadata;

    public RepoPathsFactory(IRepoMetadataParser parser, IAppPaths paths, IFileSystem fs)
    {
        _paths = paths;
        _fs = fs;
        _metadata = new Lazy<RepoMetadata>(parser.Deserialize);
    }

    private List<IDirectoryInfo> ToDirectoryInfoList(IEnumerable<string> listOfDirectories)
    {
        return listOfDirectories
            .Select(x => _paths.RepoDirectory.SubDirectory(x))
            .Where(x => x.Exists)
            .ToList();
    }

    public IRepoPaths Create()
    {
        var metadata = _metadata.Value;
        return new RepoPaths(
            ToDirectoryInfoList(metadata.JsonPaths.Radarr.CustomFormats),
            ToDirectoryInfoList(metadata.JsonPaths.Sonarr.ReleaseProfiles));
    }
}
