using System.IO.Abstractions;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.TrashLib.Repo;

public class RepoPathsFactory : IRepoPathsFactory
{
    private readonly IAppPaths _paths;
    private readonly Lazy<RepoMetadata> _metadata;

    public RepoMetadata Metadata => _metadata.Value;

    public RepoPathsFactory(IRepoMetadataParser parser, IAppPaths paths)
    {
        _paths = paths;
        _metadata = new Lazy<RepoMetadata>(parser.Deserialize);
    }

    private List<IDirectoryInfo> ToDirectoryInfoList(IEnumerable<string> listOfDirectories)
    {
        return listOfDirectories
            .Select(x => _paths.RepoDirectory.SubDirectory(x))
            .ToList();
    }

    public IRepoPaths Create()
    {
        var docs = _paths.RepoDirectory.SubDirectory("docs");
        var metadata = _metadata.Value;
        return new RepoPaths(
            ToDirectoryInfoList(metadata.JsonPaths.Radarr.CustomFormats),
            ToDirectoryInfoList(metadata.JsonPaths.Sonarr.ReleaseProfiles),
            ToDirectoryInfoList(metadata.JsonPaths.Radarr.Qualities),
            ToDirectoryInfoList(metadata.JsonPaths.Sonarr.Qualities),
            ToDirectoryInfoList(metadata.JsonPaths.Sonarr.CustomFormats),
            docs.SubDirectory("Radarr").File("Radarr-collection-of-custom-formats.md"),
            docs.SubDirectory("Sonarr").File("sonarr-collection-of-custom-formats.md")
        );
    }
}
