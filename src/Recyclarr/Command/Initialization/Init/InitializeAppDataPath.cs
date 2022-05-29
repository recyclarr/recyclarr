using System.IO.Abstractions;
using TrashLib;

namespace Recyclarr.Command.Initialization.Init;

public class InitializeAppDataPath : IServiceInitializer
{
    private readonly IFileSystem _fs;
    private readonly IAppPaths _paths;
    private readonly IDefaultAppDataSetup _appDataSetup;

    public InitializeAppDataPath(IFileSystem fs, IAppPaths paths, IDefaultAppDataSetup appDataSetup)
    {
        _fs = fs;
        _paths = paths;
        _appDataSetup = appDataSetup;
    }

    public void Initialize(ServiceCommand cmd)
    {
        _appDataSetup.SetupDefaultPath(cmd.AppDataDirectory, true);

        // Initialize other directories used throughout the application
        _fs.Directory.CreateDirectory(_paths.RepoDirectory);
        _fs.Directory.CreateDirectory(_paths.CacheDirectory);
        _fs.Directory.CreateDirectory(_paths.LogDirectory);
    }
}
