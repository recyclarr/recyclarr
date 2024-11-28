using System.IO.Abstractions;
using System.Text.Json;
using Recyclarr.Json;

namespace Recyclarr.Repo;

public class TrashRepoMetadataBuilder : IRepoMetadataBuilder
{
    private readonly Lazy<RepoMetadata> _metadata;
    private readonly IDirectoryInfo _repoPath;

    public TrashRepoMetadataBuilder(ITrashGuidesRepo repo)
    {
        _repoPath = repo.Path;
        _metadata = new Lazy<RepoMetadata>(() => Deserialize(_repoPath.File("metadata.json")));
    }

    private static RepoMetadata Deserialize(IFileInfo jsonFile)
    {
        using var stream = jsonFile.OpenRead();

        var obj = JsonSerializer.Deserialize<RepoMetadata>(
            stream,
            GlobalJsonSerializerSettings.Guide
        );
        if (obj is null)
        {
            throw new InvalidDataException($"Unable to deserialize {jsonFile}");
        }

        return obj;
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
