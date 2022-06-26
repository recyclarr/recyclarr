using System.IO.Abstractions;
using TrashLib;
using TrashLib.Cache;

namespace Recyclarr.Command.Helpers;

public class CacheStoragePath : ICacheStoragePath
{
    private readonly IAppPaths _paths;
    private readonly IActiveServiceCommandProvider _serviceCommandProvider;

    public CacheStoragePath(IAppPaths paths, IActiveServiceCommandProvider serviceCommandProvider)
    {
        _paths = paths;
        _serviceCommandProvider = serviceCommandProvider;
    }

    public string Path => _paths.CacheDirectory
        .SubDirectory(_serviceCommandProvider.ActiveCommand.Name.ToLower()).FullName;
}
