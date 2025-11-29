# TrashIdCache Design Pattern

## Purpose

Generic base class for caching trash_id → service ID mappings. Enables tracking which
TRaSH Guides resources have been synced to Sonarr/Radarr service objects.

## Class Hierarchy

```
BaseCache (holds CacheObject reference)
  └── TrashIdCache<TCacheObject> (shared mapping logic)
        ├── CustomFormatCache (CF type adapters)
        └── QualityProfileCache (future - QP type adapters)
```

## Key Types (Recyclarr.Cache namespace)

- `TrashIdMapping(TrashId, Name, ServiceId)` - shared record
- `ITrashIdCacheObject` - interface requiring `List<TrashIdMapping> Mappings { get; }`
- `TrashIdCache<T>` - generic base with FindId/RemoveStale/Update methods

## Adding a New Cache Type

1. Create CacheObject implementing ITrashIdCacheObject:
   ```csharp
   [CacheObjectName("quality-profile-cache")]
   internal record QualityProfileCacheObject() : CacheObject(1), ITrashIdCacheObject
   {
       [JsonPropertyName("...")] // if backward compat needed
       public List<TrashIdMapping> Mappings { get; set; } = [];
   }
   ```

2. Create Cache class inheriting TrashIdCache:
   ```csharp
   internal class QualityProfileCache(QualityProfileCacheObject obj)
       : TrashIdCache<QualityProfileCacheObject>(obj)
   {
       // Type-specific adapter methods
       public int? FindId(QualityProfileResource qp) => FindId(qp.TrashId);
       public void RemoveStale(IEnumerable<QualityProfileResource> profiles)
           => RemoveStale(profiles.Select(p => p.Id));
       public void Update(QualityProfileTransactionData tx) { ... }
   }
   ```

3. Create Persister subclass and register in Autofac

## Design Decisions

- Interface uses `List<T>` (not `ICollection<T>`) because implementations need
  `RemoveAll()`, `Find()`, etc. CA1002 suppressed.
- Update() uses Clear()/AddRange() to mutate list in place (no setter needed).
- `[JsonPropertyName]` used for backward compat when renaming properties.
