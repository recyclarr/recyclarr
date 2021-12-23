using LibGit2Sharp;

namespace VersionControl.Wrappers;

public interface IRepositoryStaticWrapper
{
    string Clone(string sourceUrl, string workdirPath, CloneOptions options);
    bool IsValid(string path);
}
