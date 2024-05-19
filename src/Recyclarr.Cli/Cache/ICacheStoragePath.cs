using System.IO.Abstractions;

namespace Recyclarr.Cli.Cache;

public interface ICacheStoragePath
{
    IFileInfo CalculatePath(string cacheObjectName);
}
