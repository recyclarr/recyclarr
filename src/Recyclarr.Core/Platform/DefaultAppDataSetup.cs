using System.IO.Abstractions;

namespace Recyclarr.Platform;

internal class DefaultAppDataSetup(IEnvironment env, IFileSystem fs)
{
    public IAppPaths CreateAppPaths()
    {
        CheckForDeprecatedEnvironmentVariable();

        var configDir = GetConfigDirectory();
        var dataDir = GetDataDirectory(configDir);
        var paths = new AppPaths(configDir, dataDir);
        paths.CreateTopDirectories();
        return paths;
    }

    private void CheckForDeprecatedEnvironmentVariable()
    {
        var deprecatedVar = env.GetEnvironmentVariable("RECYCLARR_APP_DATA");
        if (!string.IsNullOrEmpty(deprecatedVar))
        {
            throw new InvalidOperationException(
                """
                RECYCLARR_APP_DATA is no longer supported. Use these instead:
                  - RECYCLARR_CONFIG_DIR: User configuration (replaces APP_DATA)
                  - RECYCLARR_DATA_DIR: Ephemeral data (optional, defaults to CONFIG_DIR)

                To migrate, rename RECYCLARR_APP_DATA to RECYCLARR_CONFIG_DIR in your environment.
                """
            );
        }
    }

    private IDirectoryInfo GetConfigDirectory()
    {
        var configDir =
            env.GetEnvironmentVariable("RECYCLARR_CONFIG_DIR") ?? GetPlatformDefaultDirectory();

        return fs.Directory.CreateDirectory(configDir);
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

    private string GetPlatformDefaultDirectory()
    {
        var appData = env.GetFolderPath(
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.Create
        );

        if (string.IsNullOrEmpty(appData))
        {
            throw new NoHomeDirectoryException(
                "Unable to find or create the default app data directory. The application cannot determine where "
                    + "to place data files. Please set the RECYCLARR_CONFIG_DIR environment variable to explicitly set a location for these files."
            );
        }

        return fs.Path.Combine(appData, AppPaths.DefaultAppDataDirectoryName);
    }
}
