using System.IO.Abstractions;

namespace Recyclarr.Platform;

public class DefaultAppDataSetup(
    IEnvironment env,
    IFileSystem fs,
    IRuntimeInformation runtimeInfo)
{
    public IAppPaths CreateAppPaths(string? appDataDirectoryOverride = null)
    {
        var appDir = GetAppDataDirectory(appDataDirectoryOverride);
        return new AppPaths(fs.DirectoryInfo.New(appDir));
    }

    private string GetAppDataDirectory(string? appDataDirectoryOverride)
    {
        // If a specific app data directory is not provided, use the following environment variable to find the path.
        appDataDirectoryOverride ??= env.GetEnvironmentVariable("RECYCLARR_APP_DATA");

        // Ensure user-specified app data directory is created and use it.
        if (!string.IsNullOrEmpty(appDataDirectoryOverride))
        {
            return fs.Directory.CreateDirectory(appDataDirectoryOverride).FullName;
        }

        // Set app data path to application directory value (e.g. `$HOME/.config` on Linux) and ensure it is
        // created.
        var appData = env.GetFolderPath(
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.Create);

        if (string.IsNullOrEmpty(appData))
        {
            throw new NoHomeDirectoryException(
                "Unable to find or create the default app data directory. The application cannot determine where " +
                "to place data files. Please use the --app-data option to explicitly set a location for these files.");
        }

        appData = fs.Path.Combine(appData, AppPaths.DefaultAppDataDirectoryName);

        try
        {
            if (runtimeInfo.IsPlatformOsx())
            {
                var oldAppData = fs.Path.Combine(env.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config",
                    AppPaths.DefaultAppDataDirectoryName);
                if (fs.DirectoryInfo.New(oldAppData).Exists)
                {
                    // Attempt to move the directory for the user. If this cannot be done, then the MoveOsxAppDataDotnet8
                    // migration step (which is required) will force the issue to the user and provide remediation steps.
                    fs.Directory.Move(oldAppData, appData);
                }
            }
        }
        catch (IOException)
        {
            // Ignore failures here because we'll let the migration step take care of it.
        }

        return appData;
    }
}
