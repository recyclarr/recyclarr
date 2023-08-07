using System.Data.HashFunction.FNV;
using System.Globalization;
using System.IO.Abstractions;
using System.Text;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Interfaces;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.Cli.Console.Helpers;

public class CacheStoragePath : ICacheStoragePath
{
    private readonly ILogger _log;
    private readonly IAppPaths _paths;
    private readonly IFNV1a _hashOld;
    private readonly IFNV1a _hash;

    public CacheStoragePath(ILogger log, IAppPaths paths)
    {
        _log = log;
        _paths = paths;
        _hashOld = FNV1aFactory.Instance.Create(FNVConfig.GetPredefinedConfig(32));
        _hash = FNV1aFactory.Instance.Create(FNVConfig.GetPredefinedConfig(64));
    }

    private string BuildUniqueServiceDir(IServiceConfiguration config)
    {
        var url = config.BaseUrl.OriginalString;
        return _hash.ComputeHash(Encoding.ASCII.GetBytes(url)).AsHexString();
    }

    private string BuildOldUniqueServiceDir(IServiceConfiguration config)
    {
        var url = config.BaseUrl.OriginalString;
        var hash = _hashOld.ComputeHash(Encoding.ASCII.GetBytes(url)).AsHexString();
        return $"{config.InstanceName}_{hash}";
    }

    private IFileInfo CalculatePathInternal(IServiceConfiguration config, string cacheObjectName, string serviceDir)
    {
        return _paths.CacheDirectory
            .SubDirectory(config.ServiceType.ToString().ToLower(CultureInfo.CurrentCulture))
            .SubDirectory(serviceDir)
            .File(cacheObjectName + ".json");
    }

    public IFileInfo CalculatePath(IServiceConfiguration config, string cacheObjectName)
    {
        return CalculatePathInternal(config, cacheObjectName, BuildUniqueServiceDir(config));
    }

    public IFileInfo CalculateOldPath(IServiceConfiguration config, string cacheObjectName)
    {
        return CalculatePathInternal(config, cacheObjectName, BuildOldUniqueServiceDir(config));
    }

    public void MigrateOldPath(IServiceConfiguration config, string cacheObjectName)
    {
        var oldServiceDir = CalculateOldPath(config, cacheObjectName).Directory;
        var newServiceDir = CalculatePath(config, cacheObjectName).Directory;

        if (oldServiceDir is null || newServiceDir is null)
        {
            _log.Debug("Cache Migration: Unable to migrate cache dir due to null value for either old or new path");
            return;
        }

        if (!oldServiceDir.Exists)
        {
            _log.Debug("Cache Migration: Old path doesn't exist; skipping");
            return;
        }

        if (newServiceDir.Exists)
        {
            // New dir already exists, so we can't move. Delete it.
            _log.Debug("Cache Migration: Deleting {OldDir}", oldServiceDir);
            oldServiceDir.Delete(true);
        }
        else
        {
            // New dir doesn't exist yet; so rename old to new.
            _log.Debug("Cache Migration: Moving from {OldDir} to {NewDir}", oldServiceDir, newServiceDir);
            oldServiceDir.MoveTo(newServiceDir.FullName);
        }
    }
}
