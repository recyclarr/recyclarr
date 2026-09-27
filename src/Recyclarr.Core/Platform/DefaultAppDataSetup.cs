using System.IO.Abstractions;

namespace Recyclarr.Platform;

internal class DefaultAppDataSetup(
    IEnvironment env,
    IFileSystem fs,
    ConfigDirectoryLocator configLocator
)
{
    public IAppPaths CreateAppPaths()
    {
        var configDir = configLocator.Locate();
        configDir.Create();

        var dataDir = GetDataDirectory(configDir);
        var paths = new AppPaths(configDir, dataDir);
        paths.CreateTopDirectories();
        return paths;
    }

    private IDirectoryInfo GetDataDirectory(IDirectoryInfo configDir)
    {
        var dataDir = env.GetEnvironmentVariable("RECYCLARR_DATA_DIR") ?? configDir.FullName;

        // If data dir is relative, resolve it relative to config dir
        if (!fs.Path.IsPathRooted(dataDir))
        {
            dataDir = fs.Path.Combine(configDir.FullName, dataDir);
        }

        return fs.Directory.CreateDirectory(dataDir);
    }
}
