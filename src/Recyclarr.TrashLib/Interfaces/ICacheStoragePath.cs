using System.IO.Abstractions;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.Interfaces;

public interface ICacheStoragePath
{
    IFileInfo CalculatePath(IServiceConfiguration config, string cacheObjectName);
}
