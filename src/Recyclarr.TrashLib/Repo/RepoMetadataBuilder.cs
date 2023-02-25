using System.IO.Abstractions;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.TrashLib.Repo;

public class RepoMetadataBuilder : IRepoMetadataBuilder
{
    private readonly IAppPaths _paths;
    private readonly Lazy<RepoMetadata> _metadata;

    public RepoMetadataBuilder(IAppPaths paths)
    {
        _paths = paths;
        _metadata = new Lazy<RepoMetadata>(()
            => TrashRepoJsonParser.Deserialize<RepoMetadata>(_paths.RepoDirectory.File("metadata.json")));
    }

    public IReadOnlyList<IDirectoryInfo> ToDirectoryInfoList(IEnumerable<string> listOfDirectories)
    {
        return listOfDirectories.Select(x => _paths.RepoDirectory.SubDirectory(x)).ToList();
    }

    public IDirectoryInfo DocsDirectory => _paths.RepoDirectory.SubDirectory("docs");

    public RepoMetadata GetMetadata()
    {
        return _metadata.Value;
    }
}
