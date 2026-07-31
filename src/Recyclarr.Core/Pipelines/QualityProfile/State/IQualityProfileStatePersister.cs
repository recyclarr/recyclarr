using Recyclarr.SyncState;

namespace Recyclarr.Pipelines.QualityProfile.State;

internal interface IQualityProfileStatePersister
{
    TrashIdMappingStore Load();
    void Save(TrashIdMappingStore store);
    string StateFilePath { get; }
}
