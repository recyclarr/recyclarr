using Recyclarr.SyncState;

namespace Recyclarr.Cli.Pipelines.CustomFormat.State;

internal class CustomFormatStatePersister(ILogger log, ISyncStateStoragePath storagePath)
    : SyncStatePersister<CustomFormatMappings>(log, storagePath)
{
    protected override string StateName => "Custom Format State";
}
