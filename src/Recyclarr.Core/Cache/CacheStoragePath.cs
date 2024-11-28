using System.Data.HashFunction.FNV;
using System.Globalization;
using System.IO.Abstractions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Recyclarr.Config.Models;
using Recyclarr.Platform;

namespace Recyclarr.Cache;

public partial class CacheStoragePath(IAppPaths paths, IServiceConfiguration config)
    : ICacheStoragePath
{
    private readonly IFNV1a _hash = FNV1aFactory.Instance.Create(FNVConfig.GetPredefinedConfig(64));

    private string BuildUniqueServiceDir()
    {
        var url = config.BaseUrl.OriginalString;
        return _hash.ComputeHash(Encoding.ASCII.GetBytes(url)).AsHexString();
    }

    private IFileInfo CalculatePathInternal(string cacheObjectName, string serviceDir)
    {
        return paths
            .CacheDirectory.SubDirectory(
                config.ServiceType.ToString().ToLower(CultureInfo.CurrentCulture)
            )
            .SubDirectory(serviceDir)
            .File(cacheObjectName + ".json");
    }

    private static string GetCacheObjectNameFromAttribute<T>()
    {
        var attribute = typeof(T).GetCustomAttribute<CacheObjectNameAttribute>();
        if (attribute == null)
        {
            throw new ArgumentException(
                $"{nameof(CacheObjectNameAttribute)} is missing on type {nameof(T)}"
            );
        }

        if (!AllowedObjectNameCharactersRegex().IsMatch(attribute.Name))
        {
            throw new ArgumentException(
                $"Object name '{attribute.Name}' has unacceptable characters"
            );
        }

        return attribute.Name;
    }

    public IFileInfo CalculatePath<T>()
    {
        var cacheObjectName = GetCacheObjectNameFromAttribute<T>();
        return CalculatePathInternal(cacheObjectName, BuildUniqueServiceDir());
    }

    [GeneratedRegex(@"^[\w-]+$", RegexOptions.None, 1000)]
    private static partial Regex AllowedObjectNameCharactersRegex();
}
