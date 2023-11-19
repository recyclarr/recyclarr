using System.IO.Abstractions;
using Recyclarr.Common;

namespace Recyclarr.Platform;

public class DefaultAppDataSetup(IEnvironment env, IFileSystem fs)
{
    public IAppPaths CreateAppPaths(string? appDataDirectoryOverride = null, bool forceCreate = true)
    {
        var appDir = GetAppDataDirectory(appDataDirectoryOverride, forceCreate);
        return new AppPaths(fs.DirectoryInfo.New(appDir));
    }

    private string GetAppDataDirectory(string? appDataDirectoryOverride, bool forceCreate)
    {
        // If a specific app data directory is not provided, use the following environment variable to find the path.
        appDataDirectoryOverride ??= env.GetEnvironmentVariable("RECYCLARR_APP_DATA");

        // Ensure user-specified app data directory is created and use it.
        if (!string.IsNullOrEmpty(appDataDirectoryOverride))
        {
            return fs.Directory.CreateDirectory(appDataDirectoryOverride).FullName;
        }

        // If we can't even get the $HOME directory value, throw an exception. User must explicitly specify it with
        // --app-data.
        var home = env.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(home))
        {
            throw new NoHomeDirectoryException(
                "The system does not have a HOME directory, so the application cannot determine where to place " +
                "data files. Please use the --app-data option to explicitly set a location for these files");
        }

        // Set app data path to application directory value (e.g. `$HOME/.config` on Linux) and ensure it is
        // created.
        var appData = env.GetFolderPath(Environment.SpecialFolder.ApplicationData,
            forceCreate ? Environment.SpecialFolderOption.Create : Environment.SpecialFolderOption.None);

        if (string.IsNullOrEmpty(appData))
        {
            throw new DirectoryNotFoundException("Unable to find the default app data directory");
        }

        return fs.Path.Combine(appData, AppPaths.DefaultAppDataDirectoryName);
    }
}
