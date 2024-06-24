using System.IO.Abstractions;

namespace Recyclarr.Cache;

public interface ICacheStoragePath
{
    IFileInfo CalculatePath(string cacheObjectName);
}
