using System.IO.Abstractions;

namespace TrashLib.Cache;

public interface ICacheStoragePath
{
    IFileInfo CalculatePath(string cacheObjectName);
}
