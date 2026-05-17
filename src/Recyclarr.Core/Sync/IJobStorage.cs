namespace Recyclarr.Sync;

internal interface IJobStorage
{
    void Store(JobId jobId, string instanceName, PipelineType operationType, object? result);
    object? Retrieve(JobId jobId, string instanceName, PipelineType operationType);
    T? Retrieve<T>(JobId jobId, string instanceName, PipelineType operationType);
}
