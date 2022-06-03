using System.IO.Abstractions;
using CliFx.Exceptions;
using Common;
using TrashLib;

namespace Recyclarr.Command.Initialization;

public class DefaultAppDataSetup : IDefaultAppDataSetup
{
    private readonly IEnvironment _env;
    private readonly IAppPaths _paths;
    private readonly IFileSystem _fs;

    public DefaultAppDataSetup(IEnvironment env, IAppPaths paths, IFileSystem fs)
    {
        _env = env;
        _paths = paths;
        _fs = fs;
    }

    public void SetupDefaultPath(string? appDataDirectoryOverride, bool forceCreate)
    {
        // If a specific app data directory is not provided, use the following environment variable to find the path.
        appDataDirectoryOverride ??= _env.GetEnvironmentVariable("RECYCLARR_APP_DATA");

        // If the user did not explicitly specify an app data directory, perform some system introspection to verify if
        // the user has a home directory.
        if (string.IsNullOrEmpty(appDataDirectoryOverride))
        {
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

            _paths.SetAppDataPath(_fs.Path.Combine(appData, _paths.DefaultAppDataDirectoryName));
        }
        else
        {
            // Ensure user-specified app data directory is created and use it.
            var dir = _fs.Directory.CreateDirectory(appDataDirectoryOverride);
            _paths.SetAppDataPath(dir.FullName);
        }
    }
}
