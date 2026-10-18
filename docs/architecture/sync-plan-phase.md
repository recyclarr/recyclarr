# Sync Plan Phase

The plan phase runs before sync operations execute, validating configuration against TRaSH Guides
and building the data structures that drive synchronization. Each pipeline has one planner.

## Execution Order

Planners run in the order of `PipelineGraph`, which comes from registered pipeline dependencies
([ADR-030](../decisions/architecture/030-registered-pipeline-graph.md)). The only dependency is CF
to QP: QP planning reads the CF plan to build score assignments.

## Custom Format Planning

CF planning consolidates configuration from two sources into planned CFs:

**User-configured CFs** come from explicit `custom_formats` entries in YAML with `trash_ids` and
optional `assign_scores_to` directives.

**QP-derived CFs** come from guide-backed Quality Profiles. When a user specifies a QP by
`trash_id`, the guide resource may include `formatItems` (CF name → trash_id mappings). These are
synthesized as CF configs with `assign_scores_to` pointing to the QP, as if the user had listed them
explicitly.

Both sources merge: duplicate trash_ids consolidate, their score assignments combine. Each planned
CF carries:

- The guide resource data
- A merged list of profiles to assign scores to

Invalid trash_ids generate warnings but don't block planning.

## Quality Profile Planning

QP planning creates planned profiles from user config, building score assignments from the CF plan:

1. Match config `trash_id` to guide resources (if specified)
2. Build effective config by inheriting from guide when not specified:
   - Qualities from guide's quality items
   - `score_set` from guide's `trash_score_set`
3. Read the CF plan to build profile-to-CF-score mappings
4. Create implicit profiles for `assign_scores_to` references not in `quality_profiles`

### Reading from CF Plan

The QP component reads score assignments directly from planned CFs rather than re-parsing the
original config. This ensures QP-derived CFs (from `formatItems`) flow through correctly - they
exist in the CF plan with proper assignments even though no explicit config entry exists.

### Score Resolution

For each CF assigned to a profile:

1. Explicit `score:` in config takes priority
2. Profile's `score_set` looked up in CF's `trash_scores`
3. Fall back to CF's `default` score

The `score_set` is inherited from guide resources, so guide-backed profiles automatically use the
guide's intended scoring without explicit configuration.

### Implicit Profiles

When a CF's `assign_scores_to` references a profile name not in `quality_profiles`, an implicit
profile is created. Implicit profiles:

- Use the referenced name
- Have no guide resource
- Are marked as "should not create" (only update if exists)

## Quality Size, Media Naming, and Media Management Planning

These components have simpler planning:

**Quality Size**: Validates configured quality definitions exist in guides, resolves preferred ratio
calculations. On services that store sizes per profile (Sonarr v5), it assigns sizes to each profile
listed under `quality_profiles` instead of planning global sizes (see
[PDR-008](../decisions/product/008-sonarr-v5-per-profile-quality-sizes.md)).

**Media Naming**: Validates naming format references exist in guides for the service type.

**Media Management**: Validates propers/repacks mode configuration.

None of these depend on other planners.

## Error Collection

Planners collect errors rather than failing fast:

- Invalid trash_ids → warnings (resource continues with valid ones)
- Missing guide resources → errors (profile skipped)
- Configuration conflicts → warnings with details

All diagnostics surface after planning completes, giving users a complete picture of configuration
issues before any API interaction.
