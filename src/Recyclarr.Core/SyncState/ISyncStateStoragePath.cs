using System.IO.Abstractions;

namespace Recyclarr.SyncState;

public interface ISyncStateStoragePath
{
    IFileInfo CalculatePath(string stateName);
}
