using Recyclarr.SyncState;

namespace Recyclarr.Pipelines.CustomFormat.State;

internal interface ICustomFormatStatePersister
{
    TrashIdMappingStore Load();
    void Save(TrashIdMappingStore store);
    string StateFilePath { get; }
}
