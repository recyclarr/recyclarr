using System.Data.HashFunction.FNV;
using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using Recyclarr.Config.Models;
using Recyclarr.Platform;

namespace Recyclarr.Cli.Cache;

public class CacheStoragePath(IAppPaths paths) : ICacheStoragePath
{
    private readonly IFNV1a _hash = FNV1aFactory.Instance.Create(FNVConfig.GetPredefinedConfig(64));

    private string BuildUniqueServiceDir(IServiceConfiguration config)
    {
        var url = config.BaseUrl.OriginalString;
        return _hash.ComputeHash(Encoding.ASCII.GetBytes(url)).AsHexString();
    }

    private IFileInfo CalculatePathInternal(IServiceConfiguration config, string cacheObjectName, string serviceDir)
    {
        return paths.CacheDirectory
            .SubDirectory(config.ServiceType.ToString().ToLower(CultureInfo.CurrentCulture))
            .SubDirectory(serviceDir)
            .File(cacheObjectName + ".json");
    }

    public IFileInfo CalculatePath(IServiceConfiguration config, string cacheObjectName)
    {
        return CalculatePathInternal(config, cacheObjectName, BuildUniqueServiceDir(config));
    }
}
