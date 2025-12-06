# Guide Sync Open Questions

## Resolved

### Q: Score Set Fallback
When profile has `trash_score_set` but CF only has `default` in `trash_scores`?

**Answer:** Falls back to `DefaultScore` (from `trash_scores.default`). See
`QualityProfileConfigPhase.cs:158`.

### Q: Include vs Exclude for CF Groups
Is there an `include` inverse, or assume "include unless excluded"?

**Answer:** Include unless excluded - intentional design decision between you and nitsua.

### Q: required vs default in CF Groups
What's the semantic difference?

**Answer (from nitsua):**
- `required: true` = Must sync, user cannot disable, profile breaks without it
- `default: true` = Enabled by default, user can disable (UI)
- `required` takes precedence

### Q: formatItems vs CF Groups
Which is authoritative?

**Answer:** Complementary. `formatItems` is the direct CF→Profile assignment.
CF Groups are organizational bundles. Both contribute to final CF list.

## Open

### Q: Profile `group` Field
What are values (1, 2, 11, 81, 99) for?

**Answer (from nitsua):** Sorting/ordering weight for grouping related profiles together.
Used in TRaSH Guides to order profiles: `SORT BY "group" ASC, THEN "name" ASC`.
Groups related items (German profiles, SQP profiles, French profiles, etc.) together.
**Not needed by Recyclarr** - purely for TRaSH Guides website display ordering.

### Q: Versioning/Breaking Changes
Will there be schema versions? How to detect incompatible changes?

**Status:** Deferred. No answer available, ignore for now.

## Implementation Gotchas (from nitsua)

### Quality Name Matching
Users can rename qualities in Starr (e.g., `Bluray-1080p` → `SomethingElse`).
This breaks `cutoff` matching since JSON references canonical names.

**Solution:** If cutoff doesn't exist AND user wants guide matching:
- Treat profile as new
- Nuke quality items and recreate from JSON
- Similar to existing `reset_unmatched_scores` behavior
