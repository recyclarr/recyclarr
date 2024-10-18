using System.IO.Abstractions;

namespace Recyclarr.Cache;

public interface ICacheStoragePath
{
    IFileInfo CalculatePath<T>();
}
