# Recyclarr Caching Architecture

## Overview
Recyclarr has two distinct caching layers:
1. **Generic Cache Framework** - Application-level state caching (persisted to disk)
2. **Resource Provider Caching** - Git repository and resource loading caching

## 1. Generic Cache Framework (src/Recyclarr.Core/Cache/)

### Core Abstractions
- **CacheObject** (abstract record): Base class for all cache data models
  - Enforces versioning via `LatestVersion` property
  - Records for DTOs with JSON serialization support
  - Marked with `[CacheObjectNameAttribute]` for naming

- **TrashIdMapping** (record): Maps trash_id → name → service ID. Shared by all trash_id caches.

- **ITrashIdCacheObject** (interface): Contract for cache objects storing trash_id mappings.
  - Requires `List<TrashIdMapping> Mappings { get; }`

- **TrashIdCache<TCacheObject>** (generic base class): Shared logic for trash_id caches.
  - `FindId(trashId)` - lookup service ID by trash_id
  - `RemoveStale(validServiceIds)` - cleanup stale/duplicate mappings
  - `Update(syncedMappings, deletedIds)` - merge synced items with cache

- **CachePersister<TCacheObject, TCache>** (abstract base class):
  - Generic implementation of ICachePersister
  - Uses JSON serialization with System.Text.Json
  - Handles version mismatches via `HandleVersionMismatch()` (virtual)

- **CacheStoragePath**: Resolves cache file paths
  - Uses `[CacheObjectNameAttribute]` on types to determine filenames
  - Path structure: `cache/{ServiceType}/{InstanceHash}/{ObjectName}.json`

### Current Implementations

**CustomFormatCache** (src/Recyclarr.Cli/Pipelines/CustomFormat/Cache/):
- Inherits from `TrashIdCache<CustomFormatCacheObject>`
- Provides CF-specific type adapters for pipeline integration
- `CustomFormatCacheObject` implements `ITrashIdCacheObject`
- Uses `[JsonPropertyName("TrashIdMappings")]` for backward compat with existing cache files

### Service Registration
Registered in `CoreAutofacModule.RegisterCache()`:
- `CacheStoragePath` → `ICacheStoragePath` (singleton)

Custom format cache registered in `PipelineAutofacModule`:
- `ProcessedCustomFormatCache` → `IPipelineCache` (instance per scope)
- `CustomFormatCachePersister` → `ICachePersister<CustomFormatCache>`

## 2. Resource Provider Caching (src/Recyclarr.Core/ResourceProviders/)

### Architecture Pattern
**Strategy Pattern** with three provider types:

#### TrashGuidesStrategy
- Official GitHub repo: `https://github.com/TRaSH-Guides/Guides.git`
- Cloned to: `cache/resources/trash-guides/git/official/`
- Registers resources from metadata.json paths:
  - Custom formats (CF): `docs/json/{radarr,sonarr}/cf/`
  - Quality sizes: `docs/json/{radarr,sonarr}/quality-size/`
  - Media naming: `docs/Radarr/naming/` and `docs/Sonarr/naming/`
  - Category markdown: `docs/Radarr/Radarr-collection-of-custom-formats.md`

#### CustomFormatsStrategy
- User-provided local CF JSON files
- Path: Configured in YAML via `ResourceProvider` settings
- Service-specific: Must specify `service: radarr` or `service: sonarr`

#### ConfigTemplatesStrategy
- Recyclarr's official templates repo
- Similar Git-based caching to TrashGuides

### Resource Loading Pipeline

**ResourceRegistry<TMetadata>**:
- In-memory registry keyed by resource type (generic)
- Stores file metadata (IFileInfo objects)
- Methods: `Register<TResource>(metadata)` and `Get<TResource>()`
- SingleInstance in Autofac

**JsonResourceLoader**:
- Loads JSON files using `System.Text.Json`
- Generic method: `Load<TResource>(files)` → List of tuples
- Returns `(TResource Resource, IFileInfo SourceFile)`
- Uses `GlobalJsonSerializerSettings.Guide` for deserialization

**Resource Query Services** (singleton, injected into phases):
- `CustomFormatResourceQuery`: `GetRadarr()` / `GetSonarr()`
  - Combines CF files + category assignments
  - Deduplicates by trash_id (last occurrence wins)
- `QualitySizeResourceQuery`: Similar pattern for quality definitions
- `CategoryResourceQuery`: Parses markdown for CF categories
- `MediaNamingResourceQuery`: Loads naming conventions
- `ConfigTemplatesResourceQuery`: Config templates
- `ConfigIncludesResourceQuery`: Include templates

### Resource Models
- **CustomFormatResource** (record): Base with `TrashId`, `TrashScores`, CF data
  - Subclasses: `RadarrCustomFormatResource`, `SonarrCustomFormatResource`
  - JSON deserialization with custom converters for specs
- **QualitySizeResource** (record): Quality type + items
  - Subclasses: `RadarrQualitySizeResource`, `SonarrQualitySizeResource`
- **CategoryMarkdownResource**: Parsed from markdown files
- **MediaNamingResource**: Naming patterns per service

### Storage Layer (IProviderLocation)
**GitProviderLocation**:
- Clones/updates Git repos using IRepoUpdater
- Cache path: `appPaths.ReposDirectory/{Type}/git/{Name}/`
- Reports progress via `IProgress<ProviderProgress>`

**LocalProviderLocation**:
- Validates user-configured local paths
- Returns path if exists, empty collection otherwise

### Provider Initialization (ProviderInitializationFactory)
1. Merges default providers (TrashGuides) with user settings
2. For each provider:
   - Creates location (Git or Local)
   - Initializes async (clone/validate)
   - Calls strategy.MapResourcePaths() to register resources
3. Cleans up orphaned cache directories via ResourceCacheCleanupService

### Service Registration (ResourceProviderAutofacModule)
- `ResourceRegistry<IFileInfo>` → SingleInstance (for file mappings)
- Strategies: TrashGuidesStrategy, ConfigTemplatesStrategy, CustomFormatsStrategy
- `ProviderInitializationFactory` → SingleInstance
- Resource queries (all SingleInstance): CustomFormatResourceQuery, etc.
- Storage: GitProviderLocation, LocalProviderLocation (InstancePerDependency)

## Key Design Patterns

### Separation of Concerns
1. **Storage** (IProviderLocation): Handles Git/Local caching
2. **Infrastructure** (Strategies): Maps provider configs to resource types
3. **Domain** (Queries): Loads and processes resources
4. **Core** (CacheFramework): Generic application state persistence

### Extensibility
- New resource types: Add query class, register in Autofac
- New providers: Implement IProviderTypeStrategy
- New cache types: Extend CachePersister, mark CacheObject with attribute

### Singleton Patterns
- ResourceRegistry, JsonResourceLoader, all Query services
- Ensures single in-memory resource registry across application

### Type-Safe Queries
- Generic methods on ResourceRegistry and JsonResourceLoader
- Compile-time safety for resource type lookups
- No string-based identifiers

## Storage Paths
- App cache: `~/.config/recyclarr/cache/` (Linux)
- Resource cache: `cache/resources/trash-guides/git/official/`
- Custom format cache: `cache/{radarr,sonarr}/{instanceHash}/custom-format-cache.json`
- Metadata: `cache/resources/trash-guides/git/official/metadata.json`
