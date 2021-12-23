using LibGit2Sharp;

namespace VersionControl.Wrappers;

public class LibGit2SharpRepositoryStaticWrapper : IRepositoryStaticWrapper
{
    public string Clone(string sourceUrl, string workdirPath, CloneOptions options)
        => Repository.Clone(sourceUrl, workdirPath, options);

    public bool IsValid(string path)
        => Repository.IsValid(path);
}
