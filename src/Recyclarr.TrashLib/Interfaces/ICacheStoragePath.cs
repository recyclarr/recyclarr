using System.IO.Abstractions;
using Recyclarr.TrashLib.Config;

namespace Recyclarr.TrashLib.Interfaces;

public interface ICacheStoragePath
{
    IFileInfo CalculatePath(IServiceConfiguration config, string cacheObjectName);
    void MigrateOldPath(IServiceConfiguration config, string cacheObjectName);
}
