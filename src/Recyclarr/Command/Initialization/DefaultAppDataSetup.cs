using System.IO.Abstractions;
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

    public void SetupDefaultPath(bool forceCreate = false)
    {
        var appData = _env.GetFolderPath(Environment.SpecialFolder.ApplicationData,
            forceCreate ? Environment.SpecialFolderOption.Create : Environment.SpecialFolderOption.None);

        if (string.IsNullOrEmpty(appData))
        {
            throw new DirectoryNotFoundException("Unable to find the default app data directory");
        }

        _paths.SetAppDataPath(_fs.Path.Combine(appData, _paths.DefaultAppDataDirectoryName));
    }
}
