# Guide Sync Implementation Plan

## Motivation

Reduce config-templates maintenance burden. Currently:
- Quality profiles defined in YAML in config-templates
- Same profiles defined in JSON in trash-guides
- Two changes, two places, two formats

## Work Phases

### Phase 0: Cache Generalization (COMPLETED)

Refactored trash_id cache system to support multiple resource types.

**What was done:**
- Created `TrashIdCache<T>` generic base class with shared Find/RemoveStale/Update logic
- Created `ITrashIdCacheObject` interface and `TrashIdMapping` shared record
- `CustomFormatCache` now inherits from `TrashIdCache<CustomFormatCacheObject>`
- See `trash-id-cache-design` memory for implementation pattern

### Phase 1: Resource Loading (COMPLETED)

Load CF Groups and Quality Profiles as resources via the Resource Provider system. No pipeline
integration yet - just validate JSON parsing and set up infrastructure.

**CF Groups: COMPLETED**
- `CfGroupResource.cs` - Resource models (`CfGroupResource`, `RadarrCfGroupResource`, `SonarrCfGroupResource`)
- `CfGroupResourceQuery.cs` - Query with `GetRadarr()`/`GetSonarr()` methods
- `RepoMetadata.cs` - Added `CfGroups` property with `[JsonPropertyName("custom_format_groups")]`
- `TrashGuidesStrategy.cs` - Registered CF Group resources
- `ResourceProviderAutofacModule.cs` - Registered `CfGroupResourceQuery`
- `JsonResourceLoader.cs` - Made `JsonSerializerOptions` explicit (required for CF Groups which use Metadata settings)
- `CfGroupLoaderIntegrationTest.cs` - Integration tests passing

**Quality Profiles: COMPLETED**
- `QualityProfileResource.cs` - Resource models (`QualityProfileResource`, `QualityProfileQualityItem`,
  `RadarrQualityProfileResource`, `SonarrQualityProfileResource`)
- `QualityProfileResourceQuery.cs` - Query with `GetRadarr()`/`GetSonarr()` methods
- `RepoMetadata.cs` - Already has `QualityProfiles` property
- `TrashGuidesStrategy.cs` - Registered QP resources
- `ResourceProviderAutofacModule.cs` - Registered `QualityProfileResourceQuery`

**Key point:** CF Groups are NOT first-class YAML citizens. They're organizational metadata for
profile sync, not user-facing configuration.

### Phase 2: Pipeline Plan Architecture + Implicit Integration

**PREREQUISITE:** Refactor sync pipelines to support CF Groups. See `pipeline-plan-architecture.md`
for detailed design discussion.

**Problem:** CF Groups create chicken-and-egg dependency - CF Pipeline needs to know which CFs to
sync, but CF Group membership is determined by profile selection (QP Pipeline).

**Solution:** Two-pass architecture:
1. Plan Phase: Extract config phases into pluggable plan components, produce hierarchical
   PipelinePlan with all relationships resolved
2. Sync Phase: Pipelines consume shared PipelinePlan, hydrate with service IDs after CF Persistence

**Architecture Implementation Steps:**
1. Create PipelinePlan data structure (hierarchical: profiles → CFs)
2. Extract config phase logic into plan components
3. Add ID hydration step after CF Persistence
4. Modify Transaction phases to query PipelinePlan instead of CustomFormatLookup
5. Move cache loading from Config to Transaction phase

**Profile-by-trash_id Features (after architecture is in place):**
- Config syntax: Reference profile by `trash_id` (new) or name (existing)
- CF Group resolution: Groups apply unless profile is in `exclude` list
- Score resolution: trash_score_set → CF trash_scores lookup
- Quality items: Full hierarchy from guide JSON
- Deduplication: CFs from multiple sources merged in Plan phase

**Key behaviors:**
- `required: true` CFs must be synced (profile breaks without them)
- Groups apply unless profile is in `exclude` list
- Deduplication across all CF sources (last wins)

### Phase 3: User Overrides

YAML config to customize CF Group behavior when pulling profiles.

**Capabilities:**
- Exclude specific CF Groups from a profile
- Exclude individual CFs from groups
- Override scores for group CFs
- Include additional CF Groups not normally applicable

**Use cases:**
- "I want SQP-1 profile but skip the optional audio formats group"
- "Exclude this specific CF from all groups"
- Power-user customization while maintaining sensible defaults

### Phase 4: Config Template Evolution

Templates shift from full definitions to thin wrappers.

**New role:**
- Reference profile by trash_id
- Bundle related configs (quality-definition + profile)
- User-friendly naming/discoverability
- Override presets (stricter thresholds, etc.)

**Deprecation path:**
- Existing detailed templates continue working
- New template format references trash_ids
- Gradual migration of config-templates repo

## Testing Strategy

Each phase should be independently testable:
- Phase 1: Resource loading + JSON deserialization tests
- Phase 2: Profile sync + CF Group resolution integration tests
- Phase 3: YAML override parsing + merge logic tests
- Phase 4: Template migration compatibility tests

**Deferred tests:**
- `CfGroupResourceQuery`: Currently has no production consumer. Test when Phase 2 adds pipeline code
  that calls `GetRadarr()`/`GetSonarr()`. The higher-level integration test will exercise it.
  `JsonResourceLoader` is already tested via `CfGroupLoaderIntegrationTest`.
- `QualityProfileResourceQuery`: Same reasoning - test via Phase 2 integration tests.

## Dependencies

- Phase 1 can start immediately (minimal prerequisites)
- Phase 2 depends on Phase 1 (resources must be loadable)
- Phase 3 depends on Phase 2 (overrides modify implicit behavior)
- Phase 4 depends on Phase 2 (templates reference new profile behavior)
