using System.Text.Json;
using Recyclarr.Common.Extensions;
using Recyclarr.Json;

namespace Recyclarr.SyncState;

public abstract class SyncStatePersister<TStateObject>(
    ILogger log,
    ISyncStateStoragePath storagePath
) : ISyncStatePersister<TStateObject>
    where TStateObject : SyncStateObject, ITrashIdMappings, new()
{
    private readonly JsonSerializerOptions _jsonSettings = GlobalJsonSerializerSettings.Recyclarr;

    public TrashIdMappingStore<TStateObject> Load()
    {
        var stateData = LoadFromJson();
        if (stateData == null)
        {
            log.Debug("{StateName} does not exist; proceeding without it", StateName);
            stateData = new TStateObject();
        }

        return new TrashIdMappingStore<TStateObject>(stateData);
    }

    private TStateObject? LoadFromJson()
    {
        var path = storagePath.CalculatePath<TStateObject>();
        log.Debug("Loading {StateName} from path: {Path}", StateName, path.FullName);
        if (!path.Exists)
        {
            log.Debug("State path does not exist");
            return null;
        }

        try
        {
            using var stream = path.OpenRead();
            return JsonSerializer.Deserialize<TStateObject>(stream, _jsonSettings);
        }
        catch (JsonException e)
        {
            log.Error(e, "Failed to read state data, will proceed without state");
        }

        return null;
    }

    public void Save(TrashIdMappingStore<TStateObject> store)
    {
        var path = storagePath.CalculatePath<TStateObject>();
        log.Debug("Saving {StateName} to path {Path}", StateName, path);

        path.CreateParentDirectory();

        using var stream = path.Create();
        JsonSerializer.Serialize(stream, store.StateObject, typeof(TStateObject), _jsonSettings);
    }

    protected abstract string StateName { get; }
}
