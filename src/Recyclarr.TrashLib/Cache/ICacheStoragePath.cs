using System.IO.Abstractions;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Cache;

public interface ICacheStoragePath
{
    IFileInfo CalculatePath(IServiceConfiguration config, string cacheObjectName);
}
