using System.Data.HashFunction.FNV;
using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using Recyclarr.TrashLib.Cache;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.Cli.Command.Helpers;

public class CacheStoragePath : ICacheStoragePath
{
    private readonly IAppPaths _paths;
    private readonly IServiceCommand _serviceCommand;
    private readonly IServiceConfiguration _config;
    private readonly IFNV1a _hash;

    public CacheStoragePath(
        IAppPaths paths,
        IServiceCommand serviceCommand,
        IServiceConfiguration config)
    {
        _paths = paths;
        _serviceCommand = serviceCommand;
        _config = config;
        _hash = FNV1aFactory.Instance.Create(FNVConfig.GetPredefinedConfig(32));
    }

    private string BuildUniqueServiceDir(string? serviceName)
    {
        // In the future, once array-style configurations are removed, the service name will no longer be optional
        // and the below condition can be removed and the logic simplified.
        var dirName = new StringBuilder();
        if (serviceName is not null)
        {
            dirName.Append($"{serviceName}_");
        }

        var guid = _hash.ComputeHash(Encoding.ASCII.GetBytes(_config.BaseUrl)).AsHexString();
        dirName.Append(guid);
        return dirName.ToString();
    }

    public IFileInfo CalculatePath(string cacheObjectName)
    {
        return _paths.CacheDirectory
            .SubDirectory(_serviceCommand.Name.ToLower(CultureInfo.CurrentCulture))
            .SubDirectory(BuildUniqueServiceDir(_config.Name))
            .File(cacheObjectName + ".json");
    }
}
