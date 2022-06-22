using System.IO.Abstractions;
using Common;
using Serilog;
using TrashLib;
using TrashLib.Startup;

namespace Recyclarr;

public class ConfigurationFinder : IConfigurationFinder
{
    private readonly ILogger _log;
    private readonly IAppPaths _paths;
    private readonly IAppContext _appContext;
    private readonly IFileSystem _fs;

    public ConfigurationFinder(ILogger log, IAppPaths paths, IAppContext appContext, IFileSystem fs)
    {
        _log = log;
        _paths = paths;
        _appContext = appContext;
        _fs = fs;
    }

    public IFileInfo FindConfigPath()
    {
        var newPath = _paths.ConfigPath;
        var oldPath = _fs.DirectoryInfo.FromDirectoryName(_appContext.BaseDirectory)
            .File(AppPaths.DefaultConfigFilename);

        if (!oldPath.Exists)
        {
            return newPath;
        }

        _log.Warning(
            "`recyclarr.yml` file located adjacent to the executable is DEPRECATED. Please move it to the " +
            "following location, as support for this old location will be removed in a future release: " +
            "{NewLocation}", newPath);

        return oldPath;
    }
}
