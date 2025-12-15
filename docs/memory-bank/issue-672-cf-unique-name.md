# Issue #672: CF "Must be unique" Error Investigation

## Status

Blocked - awaiting user clarification (comment posted 2025-12-13)

## Summary

User reports "Must be unique" error when syncing custom formats with
`replace_existing_custom_formats: true`. Initial hypothesis was case-sensitivity mismatch, but
investigation found the TRaSH guide CF name matches what user claims to have created.

## Timeline

- 2025-12-13: Initial investigation started
- 2025-12-13: Verified Sonarr validation behavior via API testing
- 2025-12-13: Discovered TRaSH guide has "HULU" (not "Hulu" as user claimed)
- 2025-12-13: Posted clarification request on issue

## Key Findings

### Sonarr API Behavior (Verified via curl to local debug instance)

- Sonarr allows updating a CF's name to different case (e.g., "HULU" to "Hulu") on the SAME CF
- Sonarr allows creating multiple CFs with case-variant names ("HULU" and "Hulu" can coexist)
- "Must be unique" error only triggers when updating one CF to match another CF's exact name
- Validation code: `f.Name == c && f.Id != v.Id` (case-sensitive C# comparison)
- Database has UNIQUE constraint on Name column (Migration 171)

### TRaSH Guide Data

- Hulu CF (`f6cce30f1733d5c8194222a7507909bb`) has name "HULU" (uppercase)
- Git history confirms it has ALWAYS been "HULU", never "Hulu"
- User's claim that "the custom format that is injected uses Hulu" is incorrect

### Recyclarr Code Analysis

- `FindServiceCfByName()` uses case-insensitive matching (`EqualsIgnoreCase`)
- `IsEquivalent()` uses case-sensitive name comparison (`a.Name == b.Name`)
- `CustomFormatResource.Name` has `init` setter, so direct assignment won't work
- Any fix would need `with` expression: `guideCf = guideCf with { Name = serviceCf.Name }`

### Git History

- Case-sensitive comparison introduced in commit `59fab961` (2024-09-14)
- First released in v7.2.4
- Method was `IsEquivalentTo()`, later refactored to inline `IsEquivalent()` in `0d727fa4`

## Open Questions

1. Does user have multiple CFs with similar names (HULU, Hulu, hulu)?
2. What is the exact name of the CF user manually created?
3. Full debug/verbose logs needed to trace actual request flow

## Hypothesis

The error suggests duplicate CFs exist. Either:

- User manually created both "HULU" and "Hulu" at different times
- Some other data inconsistency in user's Sonarr instance

If user only has ONE CF named "HULU", and TRaSH guide is also "HULU", there should be no error.

## Next Steps

1. Wait for user response with clarifying information
2. Review debug/verbose logs when provided
3. Determine if this is a Recyclarr bug or user data issue
4. If bug confirmed, implement fix using `with` expression to preserve service name

## References

- Issue: https://github.com/recyclarr/recyclarr/issues/672
- Comment: https://github.com/recyclarr/recyclarr/issues/672#issuecomment-3649581312
- Sonarr validation: `src/Sonarr.Api.V3/CustomFormats/CustomFormatController.cs`
- Recyclarr transaction phase:
  `src/Recyclarr.Cli/Pipelines/CustomFormat/PipelinePhases/CustomFormatTransactionPhase.cs`
- TRaSH guide CF: `docs/json/sonarr/cf/hulu.json` (in TRaSH-Guides/Guides repo)
