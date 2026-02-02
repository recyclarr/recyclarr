using Recyclarr.SyncState;

namespace Recyclarr.Cli.Pipelines.QualityProfile.State;

internal class QualityProfileStatePersister(ILogger log, ISyncStateStoragePath storagePath)
    : SyncStatePersister<QualityProfileMappings>(log, storagePath)
{
    protected override string StateName => "Quality Profile State";
}
