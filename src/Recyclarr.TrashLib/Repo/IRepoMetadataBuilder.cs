using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Repo;

public interface IRepoMetadataBuilder
{
    RepoMetadata GetMetadata();
    IReadOnlyList<IDirectoryInfo> ToDirectoryInfoList(IEnumerable<string> listOfDirectories);
    IDirectoryInfo DocsDirectory { get; }
}
