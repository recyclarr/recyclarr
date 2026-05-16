namespace Recyclarr.Sync;

internal interface IJobStorage
{
    void Store(JobId jobId, string instanceName, PipelineType operationType, object? result);
    T? Retrieve<T>(JobId jobId, string instanceName, PipelineType operationType);
}
