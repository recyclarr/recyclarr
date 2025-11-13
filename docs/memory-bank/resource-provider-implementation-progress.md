# Resource Provider Implementation Progress

**Status:** Phase 1 - New Infrastructure Complete **Last Updated:** 2025-01-12

## Completed Work

### Phase 1: New Infrastructure (Complete)

**Storage Layer:**

- ✅ `ProviderProgress` record and `ProviderStatus` enum
- ✅ `IProviderLocation` interface
- ✅ `GitProviderLocation` implementation (uses existing `IRepoUpdater`)
- ✅ `LocalProviderLocation` implementation
- ✅ `IResourceCacheCleanupService` + implementation

**Infrastructure Layer:**

- ✅ `IResourcePathRegistry` + `ResourcePathRegistry`
- ✅ `IProviderTypeStrategy` interface
- ✅ `TrashGuidesStrategy` (registers category markdown + all resource types)
- ✅ `ConfigTemplatesStrategy` (handles templates.json and includes.json)
- ✅ `CustomFormatsStrategy` (flat directory structure)
- ✅ `ProviderInitializationFactory` with delegate factory pattern
- ✅ Autofac delegate factories: `GitLocationFactory`, `LocalLocationFactory`

**Domain Layer - Resource Models:**

- ✅ `CategoryMarkdownResource` (base + Radarr/Sonarr derived)
- ✅ `CustomFormatResource` (base + Radarr/Sonarr derived) - preserves custom equality logic
- ✅ `QualitySizeResource` (base + Radarr/Sonarr derived)
- ✅ `RadarrMediaNamingResource` (service-specific)
- ✅ `SonarrMediaNamingResource` + `SonarrEpisodeNamingResource`
- ✅ `ConfigTemplateResource` (base + Radarr/Sonarr derived) - splits TemplatePath
- ✅ `ConfigIncludeResource` (base + Radarr/Sonarr derived) - splits TemplatePath

**Domain Layer - Loaders and Queries:**

- ✅ `JsonResourceLoader` with tuple pattern: `(TResource, IFileInfo)`
- ✅ `CategoryResourceQuery` (GetRadarr/GetSonarr methods)
- ✅ `CustomFormatResourceQuery` (DRY implementation with generic helper)
- ✅ `QualitySizeResourceQuery` (DRY implementation)
- ✅ `RadarrMediaNamingResourceQuery` (separate due to different merge logic)
- ✅ `SonarrMediaNamingResourceQuery`
- ✅ `ConfigTemplateResourceQuery` (file metadata only, not contents)
- ✅ `ConfigIncludeResourceQuery`

**DI Registration:**

- ✅ `ResourceProviderAutofacModule` with all registrations
  - Infrastructure: Registry (singleton), Strategies (keyed), Factory (singleton)
  - Storage: Locations (per-dependency), Cleanup service
  - Domain: Loader (singleton), Queries (singleton)

## Files Created (24 new files)

**Storage (6 files):**

- `src/Recyclarr.Core/ResourceProvider/Storage/ProviderProgress.cs`
- `src/Recyclarr.Core/ResourceProvider/Storage/IProviderLocation.cs`
- `src/Recyclarr.Core/ResourceProvider/Storage/GitProviderLocation.cs`
- `src/Recyclarr.Core/ResourceProvider/Storage/LocalProviderLocation.cs`
- `src/Recyclarr.Core/ResourceProvider/Storage/IResourceCacheCleanupService.cs`
- `src/Recyclarr.Core/ResourceProvider/Storage/ResourceCacheCleanupService.cs`

**Infrastructure (7 files):**

- `src/Recyclarr.Core/ResourceProvider/Infrastructure/IResourcePathRegistry.cs`
- `src/Recyclarr.Core/ResourceProvider/Infrastructure/ResourcePathRegistry.cs`
- `src/Recyclarr.Core/ResourceProvider/Infrastructure/IProviderTypeStrategy.cs`
- `src/Recyclarr.Core/ResourceProvider/Infrastructure/TrashGuidesStrategy.cs`
- `src/Recyclarr.Core/ResourceProvider/Infrastructure/ConfigTemplatesStrategy.cs`
- `src/Recyclarr.Core/ResourceProvider/Infrastructure/CustomFormatsStrategy.cs`
- `src/Recyclarr.Core/ResourceProvider/Infrastructure/ProviderInitializationFactory.cs`

**Domain (10 files):**

- `src/Recyclarr.Core/ResourceProvider/Domain/CategoryMarkdownResource.cs`
- `src/Recyclarr.Core/ResourceProvider/Domain/CustomFormatResource.cs`
- `src/Recyclarr.Core/ResourceProvider/Domain/QualitySizeResource.cs`
- `src/Recyclarr.Core/ResourceProvider/Domain/RadarrMediaNamingResource.cs`
- `src/Recyclarr.Core/ResourceProvider/Domain/SonarrMediaNamingResource.cs`
- `src/Recyclarr.Core/ResourceProvider/Domain/ConfigTemplateResource.cs`
- `src/Recyclarr.Core/ResourceProvider/Domain/ConfigIncludeResource.cs`
- `src/Recyclarr.Core/ResourceProvider/Domain/JsonResourceLoader.cs`
- `src/Recyclarr.Core/ResourceProvider/Domain/CategoryResourceQuery.cs`
- `src/Recyclarr.Core/ResourceProvider/Domain/CustomFormatResourceQuery.cs`
- `src/Recyclarr.Core/ResourceProvider/Domain/QualitySizeResourceQuery.cs`
- `src/Recyclarr.Core/ResourceProvider/Domain/RadarrMediaNamingResourceQuery.cs`
- `src/Recyclarr.Core/ResourceProvider/Domain/SonarrMediaNamingResourceQuery.cs`
- `src/Recyclarr.Core/ResourceProvider/Domain/ConfigTemplateResourceQuery.cs`
- `src/Recyclarr.Core/ResourceProvider/Domain/ConfigIncludeResourceQuery.cs`

**Module (1 file):**

- `src/Recyclarr.Core/ResourceProvider/ResourceProviderAutofacModule.cs`

## Phase 2 Progress (In Progress)

### Compilation Fixes (Completed)

- ✅ Fixed ConfigTemplate/Include query instantiation (removed new() constraint, direct instantiation)
- ✅ Removed CustomFormatLoader registration from CoreAutofacModule
- ✅ Fixed TrashGuidesStrategy CloneUrl type (string → Uri)
- ✅ Fixed ConfigTemplatesStrategy CloneUrl type (string → Uri)
- ✅ Fixed CustomFormatsStrategy provider type reference (LocalResourceProvider pattern matching)
- ✅ Fixed IIndex enumeration in ProviderInitializationFactory (explicit strategy keys)
- ✅ Added ConfigTemplates namespace import to ResourceProviderAutofacModule
- ✅ Fixed query class names (ConfigTemplatesResourceQuery, ConfigIncludesResourceQuery)

### CLI Integration (Complete)

- ✅ Created ProviderProgressHandler helper class for Spectre.Console integration
- ✅ ProviderProgressHandler injects ProviderInitializationFactory (reduced DI overhead)
- ✅ Updated all 7 commands to use ProviderProgressHandler pattern:
  - SyncCommand, ConfigListTemplatesCommand, ListQualitiesCommand
  - ConfigListLocalCommand, ListMediaNamingCommand, ListCustomFormatsCommand, ConfigCreateCommand
- ✅ Fixed ProcessedQualityProfileData namespace (Recyclarr.ResourceProviders.Domain)
- ✅ All production code compiles successfully

### Test Fixes (In Progress)

**Completed:**

- ✅ NewCf.cs: CustomFormatData → CustomFormatResource
- ✅ StubRepoUpdater.cs: Updated IRepoUpdater.UpdateRepo signature
- ✅ Deleted BaseRepositoryDefinitionProviderTest (obsolete per design doc)
- ✅ Updated RepositoriesToResourceProvidersDeprecationCheckTest for new settings structure
- ✅ Updated CustomFormatLoaderIntegrationTest to use JsonResourceLoader
- ✅ Batch renamed CustomFormatData → CustomFormatResource in test files
- ✅ Added missing ResourceProviders.Domain using statements

**Remaining:**

- Test method calls to ICustomFormatsResourceQuery need updates (GetCustomFormatData → new API)
- CustomFormatResourceResult type references
- IGitRepositoryService references (obsolete)
- ~30 test errors remaining

**Code Analysis Warnings:**

- Address CA1031, CA1854, CA1860, CA1859, CA1822, CA1861 warnings
- Most are performance suggestions (TryGetValue, Count > 0, static methods)

## Phase 2 Progress (Earlier Work)

### Completed

- ✅ Fixed ConfigTemplate/Include query instantiation (added `new()` constraint and proper
  instantiation)
- ✅ Registered `ResourceProviderAutofacModule` in CompositionRoot
- ✅ Added using statement for `Recyclarr.Core.ResourceProvider`

### Completed (Partial)

**CustomFormatData → CustomFormatResource Updates:**

- ✅ API services (ICustomFormatApiService, CustomFormatApiService)
- ✅ Pipeline models (CustomFormatTransactionData, ConflictingCustomFormat,
  ProcessedCustomFormatCache)
- ✅ Cache implementations (CustomFormatCache)
- ✅ Pipeline context (CustomFormatPipelineContext)
- ✅ Pipeline phases (CustomFormatConfigPhase, CustomFormatTransactionPhase)
- ✅ Resource query adapter (CustomFormatsResourceQuery wraps CustomFormatResourceQuery)
- ✅ Result wrapper (CustomFormatDataResult)
- ✅ DeleteCustomFormatsProcessor
- ✅ CustomFormatDataLister (uses adapter, no changes)

**Status:** ALL production CustomFormatData usages complete. Remaining: test files only.

- ✅ QualityProfile models (ProcessedQualityProfileData)
- ✅ QualityProfile phases (QualityProfileConfigPhase)

## Phase 3: Old Infrastructure Cleanup (Complete)

**Deleted Files (21+ files):**

- ✅ ConfigTemplates old providers (5 files)
- ✅ TrashGuides old providers (3 files)
- ✅ Old resource provider interfaces (4 files)
- ✅ CustomFormatLoader and ICustomFormatLoader
- ✅ CustomFormatData.cs (replaced by CustomFormatResource)
- ✅ QualitySizeData.cs (replaced by QualitySizeResource)
- ✅ ConsoleGitRepositoryInitializer
- ✅ Old ResourceProviders directory (8 files: GitRepositoryService, RepositoryDefinitionProviders)
- ✅ Removed obsolete DI registrations from CompositionRoot and CoreAutofacModule
- ✅ Renamed ResourceProvider→ResourceProviders (namespace now Recyclarr.ResourceProviders.*)

## Phase 4: Additional Resource Types

**QualitySizeData → QualitySizeResource (Complete):**

- ✅ QualitySizeResourceQuery adapter (wraps new QualitySizeResourceQuery)
- ✅ IQualitySizeResourceQuery interface updated
- ✅ All production code using adapter
- ✅ QualitySizeData.cs deleted

### Remaining Work (Deferred)

**Test Files (Phase 5 - Deferred):**

- All test files still reference old types (CustomFormatData, QualitySizeData)
- Will update after production code fully stabilizes and compiles

**Other Resource Types (Not Started):**

- MediaNamingData → MediaNamingResource (8+ files)
- TemplatePath → ConfigTemplateResource/ConfigIncludeResource (5+ files)

### Strategy

Given extensive changes (50+ files), recommend:

1. Focus on core pipeline/API code first (non-test files)
2. Update test files after core code compiles
3. Checkpoint after each major rename operation

### Future Phases (Deferred)

#### Phase 3: Delete Old Infrastructure

- Remove combinatorial provider classes
- Remove old git repository service
- Remove old resource provider interfaces

#### Phase 4: Test Updates

- Replace/update BaseRepositoryDefinitionProviderTest
- Update CustomFormatLoaderIntegrationTest
- Update pipeline phase tests for resource model renames

## Key Design Decisions Made

1. **Tuple Pattern for Loaders:** `(TResource Resource, IFileInfo SourceFile)` enables
   filename-based category matching without polluting resource models
2. **Global Serializer Settings:** Using existing `GlobalJsonSerializerSettings.Guide` for all JSON
   deserialization
3. **DRY Query Implementations:** Generic private helper methods for semantically equivalent
   operations
4. **Scorched Earth Approach:** Breaking changes acceptable - all that matters is the new system
5. **Incremental Progress:** Stop at logical checkpoints for fixup commits

## Known Issues / Technical Debt

1. **ConfigTemplate/Include Query Casting:** Need to fix instantiation - currently casting
   `ConfigTemplateResource` to derived types won't work. Should use Activator.CreateInstance or
   similar.
2. **Compilation Errors Expected:** Old types still referenced throughout codebase - will need
   systematic find/replace
3. **No Tests Yet:** All code written without tests (Phase 4 work)

## Architecture Notes

**Three-Dimensional Separation:**

- Storage: Where data lives (git repos, local dirs)
- Infrastructure: How data is organized (trash-guides, config-templates, custom-formats)
- Domain: What data means (custom formats, quality sizes, naming, templates)

**Type-Based Service Identification:**

- Each service gets distinct resource model types
- Compile-time safety via type system
- No service parameters in loader/query APIs

**Precedence Model:**

- Bottom-up: Last provider in YAML wins for duplicate TrashIds
- Officials injected first (lowest precedence)
- User providers maintain YAML order
