using System.Data.HashFunction.FNV;
using System.Globalization;
using System.IO.Abstractions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Recyclarr.Config.Models;
using Recyclarr.Platform;

namespace Recyclarr.SyncState;

public partial class SyncStateStoragePath(IAppPaths paths, IServiceConfiguration config)
    : ISyncStateStoragePath
{
    private readonly IFNV1a _hash = FNV1aFactory.Instance.Create(FNVConfig.GetPredefinedConfig(64));

    private string BuildUniqueServiceDir()
    {
        var url = config.BaseUrl.OriginalString;
        return _hash.ComputeHash(Encoding.ASCII.GetBytes(url)).AsHexString();
    }

    private IFileInfo CalculatePathInternal(string stateObjectName, string serviceDir)
    {
        return paths
            .StateDirectory.SubDirectory(
                config.ServiceType.ToString().ToLower(CultureInfo.CurrentCulture)
            )
            .SubDirectory(serviceDir)
            .File(stateObjectName + ".json");
    }

    private static string GetStateObjectNameFromAttribute<T>()
    {
        var attribute = typeof(T).GetCustomAttribute<SyncStateNameAttribute>();
        if (attribute == null)
        {
            throw new ArgumentException(
                $"{nameof(SyncStateNameAttribute)} is missing on type {nameof(T)}"
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
        var stateObjectName = GetStateObjectNameFromAttribute<T>();
        return CalculatePathInternal(stateObjectName, BuildUniqueServiceDir());
    }

    [GeneratedRegex(@"^[\w-]+$", RegexOptions.None, 1000)]
    private static partial Regex AllowedObjectNameCharactersRegex();
}
