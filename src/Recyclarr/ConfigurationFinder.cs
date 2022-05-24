using System.IO.Abstractions;
using Common;
using Serilog;
using TrashLib;

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

    public string FindConfigPath()
    {
        var newPath = _paths.ConfigPath;
        var oldPath = _fs.Path.Combine(_appContext.BaseDirectory, _paths.DefaultConfigFilename);

        if (!_fs.File.Exists(oldPath))
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
