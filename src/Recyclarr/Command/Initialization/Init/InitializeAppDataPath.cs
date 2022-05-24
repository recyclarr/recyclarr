using System.IO.Abstractions;
using TrashLib;

namespace Recyclarr.Command.Initialization.Init;

public class InitializeAppDataPath : IServiceInitializer
{
    private readonly IFileSystem _fs;
    private readonly IAppPaths _paths;

    public InitializeAppDataPath(IFileSystem fs, IAppPaths paths)
    {
        _fs = fs;
        _paths = paths;
    }

    public void Initialize(ServiceCommand cmd)
    {
        if (string.IsNullOrEmpty(cmd.AppDataDirectory))
        {
            return;
        }

        _fs.Directory.CreateDirectory(cmd.AppDataDirectory);
        _paths.SetAppDataPath(cmd.AppDataDirectory);
    }
}
