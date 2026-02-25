using System.Text.Json;
using Recyclarr.Common.Extensions;
using Recyclarr.Json;

namespace Recyclarr.SyncState;

public abstract class SyncStatePersister(
    ILogger log,
    ISyncStateStoragePath storagePath,
    string stateName
)
{
    private readonly JsonSerializerOptions _jsonSettings = GlobalJsonSerializerSettings.Recyclarr;

    public TrashIdMappingStore Load()
    {
        var mappings = LoadFromJson();
        return new TrashIdMappingStore(mappings);
    }

    private List<TrashIdMapping> LoadFromJson()
    {
        var path = storagePath.CalculatePath(stateName);
        log.Debug("Loading {StateName} from path: {Path}", DisplayName, path.FullName);
        if (!path.Exists)
        {
            log.Debug("State path does not exist");
            return [];
        }

        try
        {
            using var stream = path.OpenRead();
            var container = JsonSerializer.Deserialize<MappingsContainer>(stream, _jsonSettings);
            return container?.Mappings ?? [];
        }
        catch (JsonException e)
        {
            log.Error(e, "Failed to read state data, will proceed without state");
        }

        return [];
    }

    public void Save(TrashIdMappingStore store)
    {
        var path = storagePath.CalculatePath(stateName);
        log.Debug("Saving {StateName} to path {Path}", DisplayName, path);

        path.CreateParentDirectory();

        using var stream = path.Create();
        var container = new MappingsContainer { Mappings = store.Mappings };
        JsonSerializer.Serialize(stream, container, _jsonSettings);
    }

    public string StateFilePath => storagePath.CalculatePath(stateName).FullName;

    protected abstract string DisplayName { get; }

    // Simple container for JSON serialization. The naming policy produces "mappings" from the
    // property name, matching the canonical on-disk format after migration.
    private sealed class MappingsContainer
    {
        public List<TrashIdMapping> Mappings { get; init; } = [];
    }
}
