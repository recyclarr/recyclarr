using System.Data.HashFunction.FNV;
using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using Recyclarr.TrashLib.Cache;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.Cli.Console.Helpers;

public class CacheStoragePath : ICacheStoragePath
{
    private readonly IAppPaths _paths;
    private readonly IFNV1a _hash;

    public CacheStoragePath(
        IAppPaths paths)
    {
        _paths = paths;
        _hash = FNV1aFactory.Instance.Create(FNVConfig.GetPredefinedConfig(32));
    }

    private string BuildUniqueServiceDir(IServiceConfiguration config)
    {
        // In the future, once array-style configurations are removed, the service name will no longer be optional
        // and the below condition can be removed and the logic simplified.
        var dirName = new StringBuilder();
        if (config.InstanceName is not null)
        {
            dirName.Append($"{config.InstanceName}_");
        }

        var url = config.BaseUrl.OriginalString;
        var guid = _hash.ComputeHash(Encoding.ASCII.GetBytes(url)).AsHexString();
        dirName.Append(guid);
        return dirName.ToString();
    }

    public IFileInfo CalculatePath(IServiceConfiguration config, string cacheObjectName)
    {
        return _paths.CacheDirectory
            .SubDirectory(config.ServiceType.ToString().ToLower(CultureInfo.CurrentCulture))
            .SubDirectory(BuildUniqueServiceDir(config))
            .File(cacheObjectName + ".json");
    }
}
