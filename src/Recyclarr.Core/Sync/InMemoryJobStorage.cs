using System.Collections.Concurrent;

namespace Recyclarr.Sync;

internal sealed class InMemoryJobStorage : IJobStorage
{
    private readonly ConcurrentDictionary<(JobId, string, PipelineType), object?> _storage = new();

    public void Store(
        JobId jobId,
        string instanceName,
        PipelineType operationType,
        object? result
    ) => _storage[(jobId, instanceName, operationType)] = result;

    public object? Retrieve(JobId jobId, string instanceName, PipelineType operationType) =>
        _storage.TryGetValue((jobId, instanceName, operationType), out var result) ? result : null;

    public T? Retrieve<T>(JobId jobId, string instanceName, PipelineType operationType) =>
        Retrieve(jobId, instanceName, operationType) is T typed ? typed : default;
}
