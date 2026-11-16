using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Recyclarr.Platform;

internal class DefaultAppDataSetup(
    IConfiguration configuration,
    IFileSystem fs,
    ConfigDirectoryLocator configLocator
)
{
    public IAppPaths CreateAppPaths()
    {
        var configDir = configLocator.Locate();
        var dataDir = ResolveDirectory("RECYCLARR_DATA_DIR", configDir, configDir);
        var stateDir = ResolveDirectory(
            "RECYCLARR_STATE_DIR",
            configDir,
            configDir.SubDirectory("state")
        );

        var paths = new AppPaths(configDir, dataDir, stateDir);
        paths.CreateWritableDirectories();
        return paths;
    }

    // Relative paths resolve against the config dir; empty values count as unset.
    private IDirectoryInfo ResolveDirectory(
        string variable,
        IDirectoryInfo configDir,
        IDirectoryInfo fallback
    )
    {
        var path = configuration[variable];
        if (string.IsNullOrEmpty(path))
        {
            return fallback;
        }

        return fs.DirectoryInfo.New(fs.Path.GetFullPath(path, configDir.FullName));
    }
}
