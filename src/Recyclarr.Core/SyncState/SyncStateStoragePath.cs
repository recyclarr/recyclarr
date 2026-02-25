using System.Data.HashFunction.FNV;
using System.Globalization;
using System.IO.Abstractions;
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

    private IFileInfo CalculatePathInternal(string stateName, string serviceDir)
    {
        return paths
            .StateDirectory.SubDirectory(
                config.ServiceType.ToString().ToLower(CultureInfo.CurrentCulture)
            )
            .SubDirectory(serviceDir)
            .File(stateName + ".json");
    }

    public IFileInfo CalculatePath(string stateName)
    {
        if (!AllowedObjectNameCharactersRegex().IsMatch(stateName))
        {
            throw new ArgumentException($"State name '{stateName}' has unacceptable characters");
        }

        return CalculatePathInternal(stateName, BuildUniqueServiceDir());
    }

    [GeneratedRegex(@"^[\w-]+$", RegexOptions.None, 1000)]
    private static partial Regex AllowedObjectNameCharactersRegex();
}
