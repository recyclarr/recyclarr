using System.Collections.Concurrent;

namespace Recyclarr.Sync;

internal sealed class InMemorySyncRunStorage : ISyncRunStorage
{
    private readonly ConcurrentDictionary<(string, PipelineType), object?> _storage = new();

    public void Store(string instanceName, PipelineType operationType, object? result) =>
        _storage[(instanceName, operationType)] = result;

    public object? Retrieve(string instanceName, PipelineType operationType) =>
        _storage.TryGetValue((instanceName, operationType), out var result) ? result : null;

    public T? Retrieve<T>(string instanceName, PipelineType operationType) =>
        Retrieve(instanceName, operationType) is T typed ? typed : default;
}
