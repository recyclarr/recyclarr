using System.IO.Abstractions;

namespace Recyclarr.Platform;

/// <summary>
/// Resolves the Recyclarr configuration directory: <c>RECYCLARR_CONFIG_DIR</c> when set, otherwise
/// <c>recyclarr</c> under the platform's application data folder.
/// </summary>
/// <remarks>
/// Every Recyclarr executable on one machine reads its configuration files from this directory,
/// so all of them must resolve it through this type. The directory is not created; callers that
/// write to it create it themselves. Throws <see cref="EnvironmentException"/> when the retired
/// <c>RECYCLARR_APP_DATA</c> variable is set or no platform default exists.
/// </remarks>
public class ConfigDirectoryLocator(IEnvironment env, IFileSystem fs)
{
    public const string DefaultDirectoryName = "recyclarr";

    public IDirectoryInfo Locate()
    {
        RejectRetiredEnvironmentVariable();

        var configDir =
            env.GetEnvironmentVariable("RECYCLARR_CONFIG_DIR") ?? GetPlatformDefaultDirectory();

        return fs.DirectoryInfo.New(configDir);
    }

    private void RejectRetiredEnvironmentVariable()
    {
        var deprecatedVar = env.GetEnvironmentVariable("RECYCLARR_APP_DATA");
        if (string.IsNullOrEmpty(deprecatedVar))
        {
            return;
        }

        throw new EnvironmentException(
            """
            RECYCLARR_APP_DATA is no longer supported. Use these instead:
              - RECYCLARR_CONFIG_DIR: User configuration (replaces APP_DATA)
              - RECYCLARR_DATA_DIR: Ephemeral data (optional, defaults to CONFIG_DIR)

            To migrate, rename RECYCLARR_APP_DATA to RECYCLARR_CONFIG_DIR in your environment.
            """
        );
    }

    private string GetPlatformDefaultDirectory()
    {
        var appData = env.GetFolderPath(
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.Create
        );

        if (string.IsNullOrEmpty(appData))
        {
            throw new EnvironmentException(
                "Unable to find or create the default app data directory. The application cannot "
                    + "determine where to place data files. Please set the RECYCLARR_CONFIG_DIR "
                    + "environment variable to explicitly set a location for these files."
            );
        }

        return fs.Path.Combine(appData, DefaultDirectoryName);
    }
}
