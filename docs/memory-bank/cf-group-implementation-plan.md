# Implementation Plan: Custom Format Groups Support

Design doc: `docs/memory-bank/cf-group-support.md`

## Workflow

**Stop after each commit cycle.** Discuss any surprises before proceeding.

**Cycle pattern:**
1. Write code
2. Run code coverage scripts
3. Write tests based on coverage gaps
4. Commit

## Existing Infrastructure (No Changes Needed)

- `CfGroupResource` at `src/Recyclarr.Core/ResourceProviders/Domain/CfGroupResource.cs`
- `CfGroupResourceQuery` at `src/Recyclarr.Core/ResourceProviders/Domain/CfGroupResourceQuery.cs`

---

## Commit 1: Add TrashId to assign_scores_to [DONE]

**Scope:** Allow `assign_scores_to` to reference profiles by `trash_id` as alternative to `name`.

**Files:**

- `src/Recyclarr.Core/Config/Parsing/ConfigYamlDataObjects.cs` - Add `TrashId` to `QualityScoreConfigYaml`
- `src/Recyclarr.Core/Config/Models/ServiceConfiguration.cs` - Add `TrashId` to `AssignScoresToConfig`
- `src/Recyclarr.Core/Config/Parsing/ConfigYamlExtensions.cs` - Update transformation
- `src/Recyclarr.Core/Config/Parsing/ConfigYamlDataObjectsValidation.cs` - Require Name OR TrashId
- `src/Recyclarr.Core/Config/Parsing/PostProcessing/ConfigMerging/ServiceConfigMerger.cs` - Update `FlattenedCfs` key
- `schemas/config/custom-formats.json` - Add `trash_id` property

**Test:** Existing tests pass + new test for TrashId-based assignment

---

## Commit 2: Update quality_profiles merge key [DONE]

**Scope:** Change merge key from `Name` only to composite `TrashId ?? Name`.

**Files:**

- `src/Recyclarr.Core/Config/Parsing/PostProcessing/ConfigMerging/ServiceConfigMerger.cs` - Line ~124

**Test:** New merge test verifying profiles join by trash_id when present

---

## Commit 3: CF Groups config models + YAML parsing [DONE]

**Scope:** Add domain and YAML models for CF group configuration.

**Files:**

- `src/Recyclarr.Core/Config/Models/ServiceConfiguration.cs` - Add records:
  - `CfGroupAssignScoresToConfig` with `TrashId`
  - `CustomFormatGroupConfig` with `TrashId`, `AssignScoresTo`, `Exclude`
- `src/Recyclarr.Core/Config/Models/IServiceConfiguration.cs` - Add interface property
- `src/Recyclarr.Core/Config/Parsing/ConfigYamlDataObjects.cs` - Add YAML records
- `src/Recyclarr.Core/Config/Parsing/ConfigYamlExtensions.cs` - Add transformations

**Test:** Unit test parsing CF group YAML into models

---

## Commit 4: CF Groups schema + merge logic [DONE]

**Scope:** JSON schema validation and include merge semantics.

**Files:**

- `schemas/config/custom-format-groups.json` (NEW)
- `schemas/config-schema.json` - Add reference to both service instances
- `src/Recyclarr.Core/Config/Parsing/PostProcessing/ConfigMerging/ServiceConfigMerger.cs` - Add `MergeCustomFormatGroups`

**Merge Semantics:** Join by `trash_id`, Replace for `exclude` and `assign_scores_to`

**Test:** Merge tests (join by trash_id, replace semantics)

---

## Commit 5: CF Groups provider integration [DONE]

**Scope:** Resolve CF groups to CustomFormatConfig entries.

**Files:**

- `src/Recyclarr.Cli/Pipelines/CustomFormat/ConfiguredCustomFormatProvider.cs`
  - Add `CfGroupResourceQuery` and `ILogger` dependencies
  - Add `FromCfGroups()` method with debug logging
  - Add `DetermineProfiles()` helper for explicit/implicit assignment
  - Update `GetAll()` to include third source
- `tests/Recyclarr.Cli.Tests/Pipelines/Plan/PlanBuilderIntegrationTest.cs`
  - Add `SetupCfGroupGuideData()` helper
  - Add 6 integration tests covering all CF group scenarios
- `tests/Recyclarr.Core.TestLibrary/MockFileSystemExtensions.cs`
  - Renamed from `MockFileSystemYamlExtensions`
  - Add `AddJsonFile()` extension method

**Resolution Logic:**

1. Resolve group trash_id to `CfGroupResource`
2. Filter CFs by `exclude` list
3. Determine profiles: explicit `assign_scores_to` OR all guide-backed profiles
4. Respect group's JSON `quality_profiles.exclude`
5. Resolve profile trash_ids to names for plan component compatibility

**Test:** 6 integration tests with 100% coverage on provider

---

## Commit 6: CF Groups validation

**Scope:** Validate CF group configuration against guide resources.

**Files:**

- `src/Recyclarr.Core/Config/Parsing/ConfigYamlDataObjectsValidation.cs` - YAML validators
- `src/Recyclarr.Cli/Pipelines/CustomFormat/CfGroupValidator.cs` (NEW) - Resource validation

**Validation Scenarios:**

1. Invalid group trash_id (doesn't exist in guide)
2. Excluded required CF (CF with `required: true`)
3. Invalid CF trash_id in exclude
4. Profile excluded by group JSON
5. Invalid profile trash_id in assign_scores_to

**Test:** Validation tests for each error scenario

---

## Critical Files Reference

- `src/Recyclarr.Cli/Pipelines/CustomFormat/ConfiguredCustomFormatProvider.cs` - Core resolution
- `src/Recyclarr.Core/Config/Parsing/PostProcessing/ConfigMerging/ServiceConfigMerger.cs` - Merge logic
- `src/Recyclarr.Core/Config/Parsing/ConfigYamlDataObjects.cs` - YAML models
- `src/Recyclarr.Core/Config/Models/ServiceConfiguration.cs` - Domain models
- `src/Recyclarr.Core/ResourceProviders/Domain/CfGroupResource.cs` - Resource structure reference
