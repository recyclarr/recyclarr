using System.IO.Abstractions;
using System.Linq;

namespace Trash;

public class LogJanitor : ILogJanitor
{
    private readonly IFileSystem _fileSystem;

    public LogJanitor(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void DeleteOldestLogFiles(int numberOfNewestToKeep)
    {
        foreach (var file in _fileSystem.DirectoryInfo.FromDirectoryName(AppPaths.LogDirectory).GetFiles()
                     .OrderByDescending(f => f.Name)
                     .Skip(numberOfNewestToKeep))
        {
            file.Delete();
        }
    }
}
