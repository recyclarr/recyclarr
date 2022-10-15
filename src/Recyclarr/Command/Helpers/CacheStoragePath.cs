using System.IO.Abstractions;
using TrashLib.Cache;
using TrashLib.Startup;

namespace Recyclarr.Command.Helpers;

public class CacheStoragePath : ICacheStoragePath
{
    private readonly IAppPaths _paths;
    private readonly IServiceCommand _serviceCommand;

    public CacheStoragePath(IAppPaths paths, IServiceCommand serviceCommand)
    {
        _paths = paths;
        _serviceCommand = serviceCommand;
    }

    public string Path => _paths.CacheDirectory
        .SubDirectory(_serviceCommand.Name.ToLower()).FullName;
}
