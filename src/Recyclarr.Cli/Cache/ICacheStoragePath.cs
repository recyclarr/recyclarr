using System.IO.Abstractions;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.Cache;

public interface ICacheStoragePath
{
    IFileInfo CalculatePath(IServiceConfiguration config, string cacheObjectName);
}
