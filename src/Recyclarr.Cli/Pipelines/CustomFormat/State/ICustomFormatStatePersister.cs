using Recyclarr.SyncState;

namespace Recyclarr.Cli.Pipelines.CustomFormat.State;

internal interface ICustomFormatStatePersister
{
    TrashIdMappingStore Load();
    void Save(TrashIdMappingStore store);
    string StateFilePath { get; }
}
