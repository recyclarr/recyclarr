namespace Recyclarr.Sync;

/// <summary>
/// Holds what each pipeline computed during one sync run, so results can be read back after the
/// run finishes. Scoped to the run, which is why nothing here is keyed by a run identity.
/// </summary>
internal interface ISyncRunStorage
{
    void Store(string instanceName, PipelineType operationType, object? result);
    object? Retrieve(string instanceName, PipelineType operationType);
    T? Retrieve<T>(string instanceName, PipelineType operationType);
}
