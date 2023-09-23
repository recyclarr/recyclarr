using System.IO.Abstractions;
using Recyclarr.Config.Models;

namespace Recyclarr.TrashLib.Interfaces;

public interface ICacheStoragePath
{
    IFileInfo CalculatePath(IServiceConfiguration config, string cacheObjectName);
}
