using Recyclarr.SyncState;

namespace Recyclarr.Cli.Pipelines.QualityProfile.State;

internal class QualityProfileStatePersister(ILogger log, ISyncStateStoragePath storagePath)
    : SyncStatePersister(log, storagePath, "quality-profile-mappings"),
        IQualityProfileStatePersister
{
    protected override string DisplayName => "Quality Profile State";
}
