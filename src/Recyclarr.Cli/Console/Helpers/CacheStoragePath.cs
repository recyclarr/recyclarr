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
    private readonly IServiceConfiguration _config;
    private readonly IFNV1a _hash;

    public CacheStoragePath(
        IAppPaths paths,
        IServiceConfiguration config)
    {
        _paths = paths;
        _config = config;
        _hash = FNV1aFactory.Instance.Create(FNVConfig.GetPredefinedConfig(32));
    }

    private string BuildUniqueServiceDir()
    {
        // In the future, once array-style configurations are removed, the service name will no longer be optional
        // and the below condition can be removed and the logic simplified.
        var dirName = new StringBuilder();
        if (_config.InstanceName is not null)
        {
            dirName.Append($"{_config.InstanceName}_");
        }

        var url = _config.BaseUrl.OriginalString;
        var guid = _hash.ComputeHash(Encoding.ASCII.GetBytes(url)).AsHexString();
        dirName.Append(guid);
        return dirName.ToString();
    }

    public IFileInfo CalculatePath(string cacheObjectName)
    {
        return _paths.CacheDirectory
            .SubDirectory(_config.ServiceName.ToLower(CultureInfo.CurrentCulture))
            .SubDirectory(BuildUniqueServiceDir())
            .File(cacheObjectName + ".json");
    }
}
