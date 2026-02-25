using Recyclarr.SyncState;

namespace Recyclarr.Cli.Pipelines.CustomFormat.State;

internal class CustomFormatStatePersister(ILogger log, ISyncStateStoragePath storagePath)
    : SyncStatePersister(log, storagePath, "custom-format-mappings"),
        ICustomFormatStatePersister
{
    protected override string DisplayName => "Custom Format State";
}
