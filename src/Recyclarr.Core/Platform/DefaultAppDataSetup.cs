using System.IO.Abstractions;

namespace Recyclarr.Platform;

public class DefaultAppDataSetup(IEnvironment env, IFileSystem fs) : IAppDataSetup
{
    private string? _appDataDirectoryOverride;

    public void SetAppDataDirectoryOverride(string path)
    {
        _appDataDirectoryOverride = path;
    }

    public IAppPaths CreateAppPaths()
    {
        var appDir = GetAppDataDirectory(_appDataDirectoryOverride);
        var paths = new AppPaths(fs.DirectoryInfo.New(appDir));

        // Initialize other directories used throughout the application
        // Do not initialize the repo directory here; the RepoUpdater handles that later.
        paths.CacheDirectory.Create();
        paths.LogDirectory.Create();
        paths.ConfigsDirectory.Create();
        paths.IncludesDirectory.Create();

        return paths;
    }

    private string GetAppDataDirectory(string? appDataDirectoryOverride)
    {
        if (string.IsNullOrEmpty(appDataDirectoryOverride))
        {
            // If a specific app data directory is not provided, use the following environment variable to find the path.
            appDataDirectoryOverride = env.GetEnvironmentVariable("RECYCLARR_APP_DATA") ?? "";
        }

        // Ensure user-specified app data directory is created and use it.
        if (!string.IsNullOrEmpty(appDataDirectoryOverride))
        {
            return fs.Directory.CreateDirectory(appDataDirectoryOverride).FullName;
        }

        // Set app data path to application directory value (e.g. `$HOME/.config` on Linux) and ensure it is
        // created.
        var appData = env.GetFolderPath(
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.Create
        );

        if (string.IsNullOrEmpty(appData))
        {
            throw new NoHomeDirectoryException(
                "Unable to find or create the default app data directory. The application cannot determine where "
                    + "to place data files. Please use the --app-data option to explicitly set a location for these files."
            );
        }

        return fs.Path.Combine(appData, AppPaths.DefaultAppDataDirectoryName);
    }
}
