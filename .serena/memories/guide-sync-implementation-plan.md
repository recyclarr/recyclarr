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

### Phase 1: CF Groups

Lower complexity starting point.

**Implementation:**
- New resource type: `CfGroupResource`
- New query: `CfGroupResourceQuery`
- Loading: Parse JSON, register in `ResourceRegistry`
- Resolution: When syncing profile, find applicable groups (not in exclude list)
- Merge: Combine group CFs with explicit config CFs

**Key behaviors:**
- `required: true` CFs must be synced
- Groups apply unless profile is in `exclude` list
- Deduplication needed (CF may appear in multiple sources)

### Phase 2: Quality Profiles

Higher complexity - full profile definition from JSON.

**Implementation:**
- New resource type: `QualityProfileResource`
- New query: `QualityProfileResourceQuery`
- New cache: Profile trash_id → service ID mapping (like CF cache)
- Config syntax: Reference profile by `trash_id` or name
- Quality items: Full hierarchy from JSON (groups, allowed flags)
- formatItems: Implicit CF assignment

**Key behaviors:**
- Quality name matching gotcha (nitsua): Users can rename qualities in Starr
- Solution: "Reset quality items" mode - nuke and recreate if cutoff doesn't match
- Score resolution: trash_score_set → CF trash_scores lookup

### Phase 3: Config Template Evolution

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
- Phase 1: CF group loading + resolution unit tests
- Phase 2: Profile loading + quality item sync integration tests
- Phase 3: Template migration compatibility tests

## Dependencies

- Phase 1 can start immediately (minimal prerequisites)
- Phase 2 depends on Phase 1 (CF groups inform profile CF list)
- Phase 3 depends on Phase 2 (templates reference new profile behavior)
