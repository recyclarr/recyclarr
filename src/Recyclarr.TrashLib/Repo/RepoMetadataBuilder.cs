using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Repo;

public class RepoMetadataBuilder : IRepoMetadataBuilder
{
    private readonly Lazy<RepoMetadata> _metadata;
    private readonly IDirectoryInfo _repoPath;

    public RepoMetadataBuilder(ITrashGuidesRepo repo)
    {
        _repoPath = repo.Path;
        _metadata = new Lazy<RepoMetadata>(
            () => TrashRepoJsonParser.Deserialize<RepoMetadata>(_repoPath.File("metadata.json")));
    }

    public IReadOnlyList<IDirectoryInfo> ToDirectoryInfoList(IEnumerable<string> listOfDirectories)
    {
        return listOfDirectories.Select(x => _repoPath.SubDirectory(x)).ToList();
    }

    public IDirectoryInfo DocsDirectory => _repoPath.SubDirectory("docs");

    public RepoMetadata GetMetadata()
    {
        return _metadata.Value;
    }
}
