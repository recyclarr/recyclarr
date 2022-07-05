using System.IO.Abstractions;
using CliFx.Exceptions;
using Common;

namespace TrashLib.Startup;

public class DefaultAppDataSetup
{
    private readonly IEnvironment _env;
    private readonly IFileSystem _fs;

    public DefaultAppDataSetup(IEnvironment env, IFileSystem fs)
    {
        _env = env;
        _fs = fs;
    }

    public IAppPaths CreateAppPaths(string? appDataDirectoryOverride = null, bool forceCreate = true)
    {
        var appDir = GetAppDataDirectory(appDataDirectoryOverride, forceCreate);
        return new AppPaths(_fs.DirectoryInfo.FromDirectoryName(appDir));
    }

    private string GetAppDataDirectory(string? appDataDirectoryOverride, bool forceCreate)
    {
        // If a specific app data directory is not provided, use the following environment variable to find the path.
        appDataDirectoryOverride ??= _env.GetEnvironmentVariable("RECYCLARR_APP_DATA");

        // Ensure user-specified app data directory is created and use it.
        if (!string.IsNullOrEmpty(appDataDirectoryOverride))
        {
            return _fs.Directory.CreateDirectory(appDataDirectoryOverride).FullName;
        }

        // If we can't even get the $HOME directory value, throw an exception. User must explicitly specify it with
        // --app-data.
        var home = _env.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(home))
        {
            throw new CommandException(
                "The system does not have a HOME directory, so the application cannot determine where to place " +
                "data files. Please use the --app-data option to explicitly set a location for these files.");
        }

        // Set app data path to application directory value (e.g. `$HOME/.config` on Linux) and ensure it is
        // created.
        var appData = _env.GetFolderPath(Environment.SpecialFolder.ApplicationData,
            forceCreate ? Environment.SpecialFolderOption.Create : Environment.SpecialFolderOption.None);

        if (string.IsNullOrEmpty(appData))
        {
            throw new DirectoryNotFoundException("Unable to find the default app data directory");
        }

        return _fs.Path.Combine(appData, AppPaths.DefaultAppDataDirectoryName);
    }
}
