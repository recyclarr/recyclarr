using System.IO.Abstractions;

namespace Recyclarr.TrashLib.Cache;

public interface ICacheStoragePath
{
    IFileInfo CalculatePath(string cacheObjectName);
}
