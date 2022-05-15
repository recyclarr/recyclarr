using System.IO.Abstractions;
using TrashLib;

namespace Recyclarr.Logging;

public class LogJanitor : ILogJanitor
{
    private readonly IFileSystem _fs;
    private readonly IAppPaths _paths;

    public LogJanitor(IFileSystem fs, IAppPaths paths)
    {
        _fs = fs;
        _paths = paths;
    }

    public void DeleteOldestLogFiles(int numberOfNewestToKeep)
    {
        var dir = _fs.Directory.CreateDirectory(_paths.LogDirectory);

        foreach (var file in dir.GetFiles()
                     .OrderByDescending(f => f.Name)
                     .Skip(numberOfNewestToKeep))
        {
            file.Delete();
        }
    }
}
