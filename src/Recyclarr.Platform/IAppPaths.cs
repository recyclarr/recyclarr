using System.IO.Abstractions;

namespace Recyclarr.Platform;

public interface IAppPaths
{
    IDirectoryInfo AppDataDirectory { get; }
    IDirectoryInfo LogDirectory { get; }
    IDirectoryInfo ReposDirectory { get; }
    IDirectoryInfo CacheDirectory { get; }
    IDirectoryInfo ConfigsDirectory { get; }
    IDirectoryInfo IncludesDirectory { get; }
}
