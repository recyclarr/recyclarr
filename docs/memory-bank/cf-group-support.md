# Custom Format Group Support - Design Document

## Summary

Add support for CF groups from TRaSH Guides, allowing users to opt into collections of related
custom formats with simplified configuration.

## YAML Schema

```yaml
custom_format_groups:
  - trash_id: <group-trash-id>
    assign_scores_to:  # optional; omit = all guide-backed profiles
      - trash_id: <profile-trash-id>
    exclude:  # optional; omit = get all CFs in group
      - <optional-cf-trash-id>
```

## Semantics

### Layer 1: Group Opt-In

- All groups are opt-in only (user must explicitly list them)
- Group-level `default` property from guide JSON is ignored
- Service-specific: Radarr groups only work with Radarr configs (and vice versa)

### Layer 2: CF Selection Within Group

- When opted in, user gets ALL CFs: required + optional
- CF-level `default` property from guide JSON is ignored
- `exclude` removes specific optional CFs
- Excluding a CF with `required: true` is a validation error

### Profile Assignment

**Implicit (no `assign_scores_to`):**

- Auto-assigns to ALL guide-backed profiles in config (those with `trash_id`)
- Respects group's JSON `quality_profiles.exclude` list
- User-defined profiles (no `trash_id`) are silently skipped

**Explicit (`assign_scores_to` present):**

- Only applies to listed profiles
- `assign_scores_to` only accepts `trash_id` (not `name`)
- Still validates against group's JSON `quality_profiles.exclude`

### Scoring

CFs from groups use the same scoring mechanism as regular CFs:

- `trash_scores` from CF JSON
- Resolved via profile's `score_set`
- No special handling needed

## Validation Scenarios

1. **Invalid group trash_id** - Group trash_id doesn't exist in guide resources (also catches
   service mismatch since queries are service-specific)
2. **Excluded required CF** - User's `exclude` list contains a CF with `required: true`
3. **Invalid CF trash_id in exclude** - CF trash_id in `exclude` doesn't exist in the group
4. **Profile excluded by group** - `assign_scores_to` references a profile in the group's JSON
   `quality_profiles.exclude`
5. **Invalid profile trash_id** - `assign_scores_to` references a trash_id not in config's
   guide-backed profiles

## Include Template Merge Semantics

CF groups follow the same merge pattern as `custom_formats`.

### Top-Level Merge Reference

| Property Name          | Node Type | Merge Operation |
| ---------------------- | --------- | --------------- |
| `custom_format_groups` | Sequence  | Join            |

### Join Behavior

Items are joined by `trash_id`. When the same group `trash_id` appears on both sides:

- Side B (root config) takes precedence
- Allows users to override `exclude` lists from included templates
- Allows users to override `assign_scores_to` from included templates

### Property Merge Operations (Within Joined Item)

| Property Name      | Node Type | Merge Operation |
| ------------------ | --------- | --------------- |
| `trash_id`         | Scalar    | Replace         |
| `assign_scores_to` | Sequence  | Replace         |
| `exclude`          | Sequence  | Replace         |

Note: `assign_scores_to` and `exclude` use Replace (not Add) to give users full control over
overriding included templates.

### Example

```yaml
# include.yml
custom_format_groups:
  - trash_id: streaming-services
    exclude:
      - peacock
      - paramount-plus

# config.yml (root)
include:
  - config: include.yml

custom_format_groups:
  - trash_id: streaming-services
    exclude:
      - hulu  # User wants different exclusions

# Result: User's exclude list completely replaces include's
custom_format_groups:
  - trash_id: streaming-services
    exclude:
      - hulu
```

## Implementation

### Files to Modify

**Config Models:**

- `src/Recyclarr.Core/Config/Models/ServiceConfiguration.cs` - Add `CustomFormatGroupConfig` record
  and property
- `src/Recyclarr.Core/Config/Models/IServiceConfiguration.cs` - Add property to interface

**YAML Parsing:**

- `src/Recyclarr.Core/Config/Parsing/ConfigYamlDataObjects.cs` - Add `CustomFormatGroupConfigYaml`
  record and property to `ServiceConfigYaml`

**Schema:**

- `schemas/config-schema.json` - Add `custom_format_groups` schema

**Include Merge:**

- `src/Recyclarr.Core/Config/Parsing/PostProcessing/ConfigMerging/` - Add merge logic for CF groups

**Provider:**

- `src/Recyclarr.Cli/Pipelines/CustomFormat/ConfiguredCustomFormatProvider.cs` - Add third source
  for CF groups

**Validation:**

- Add validators for the 5 validation scenarios

### Pipeline Impact

The pipeline itself stays unchanged. `CustomFormatPlanComponent` consumes `cfProvider.GetAll()` and
processes whatever comes out. CF groups are resolved to `CustomFormatConfig` entries in the
provider.

## Test Scenarios (Happy Path First)

### Integration Tests

1. **CF group resolves to planned CFs** - Verify group CFs appear in plan with correct profile
   assignments
2. **Implicit profile assignment** - Group with no `assign_scores_to` applies to all guide-backed
   profiles
3. **Explicit profile assignment** - Group with `assign_scores_to` only applies to listed profiles
4. **Exclude filters optional CFs** - User's exclude list removes CFs from group
5. **Group respects profile exclusion** - Group doesn't apply to profiles in its JSON exclude list

### Merge Tests

1. **CF groups merge via join** - Same trash_id from both sides merges correctly
2. **Exclude list replaces on merge** - Root config exclude completely replaces include's exclude
3. **assign_scores_to replaces on merge** - Root config assignment completely replaces include's

### Validation Tests (After Coverage Analysis)

Tests for each validation scenario as needed based on code coverage gaps.

## Guide JSON Structure Reference

From TRaSH Guides CONTRIBUTING.md:

```json
{
  "trash_id": "group-hash",
  "name": "Group Name",
  "trash_description": "Description",
  "default": "true",
  "custom_formats": [
    {
      "name": "CF Name",
      "trash_id": "cf-hash",
      "required": true,
      "default": false
    }
  ],
  "quality_profiles": {
    "exclude": {
      "Profile Name": "profile-trash-id"
    }
  }
}
```

## Related Merge Updates (Pre-requisite Work)

These merge logic updates are needed before or alongside CF group support.

### `quality_profiles` Merge Key Update

**File:** `src/Recyclarr.Core/Config/Parsing/PostProcessing/ConfigMerging/ServiceConfigMerger.cs`

**Current:** Joins by `Name` only (line 124-131)

**Required:** Join by composite key - `trash_id` if present, else `name`

```csharp
// Key selector should be:
x => x.TrashId ?? x.Name
```

**Wiki Update:** Add `trash_id` as alternative join key in merge reference table.

### `custom_formats.assign_scores_to` TrashId Support

**Current:** `QualityScoreConfigYaml` only has `Name` property

**Required:** Add `TrashId` property (optional, legacy `Name` still supported)

```yaml
# Both valid:
assign_scores_to:
  - name: HD-1080p          # Legacy
  - trash_id: abc123        # New
```

**Merge Impact:** `MergeCustomFormats` / `FlattenedCfs` needs to match by `TrashId` OR `Name`

**Wiki Update:** Document that `assign_scores_to` now accepts `trash_id` as alternative to `name`.

### Separate Models

- `custom_formats.assign_scores_to` → `QualityScoreConfigYaml` with both `Name` and `TrashId`
- `custom_format_groups.assign_scores_to` → `CfGroupAssignScoresToConfigYaml` with `TrashId` only

## Changelog Entry

```markdown
### Added

- Custom Formats: Support for `custom_format_groups` to sync TRaSH Guide CF groups. Groups bundle
  related custom formats with automatic profile assignment. Required CFs are always included;
  optional CFs can be excluded via the `exclude` list.
- Custom Formats: `assign_scores_to` now accepts `trash_id` as an alternative to `name` for
  referencing quality profiles.
```
