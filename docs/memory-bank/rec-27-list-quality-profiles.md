# REC-27: List Quality Profiles Command

## Status

UI design in progress - awaiting user selection from mockup options.

## Issue

Add `recyclarr list quality-profiles <service_type>` command following existing list subcommand
conventions.

## Key Design Decisions

### Data Fields to Display

- Name (from `QualityProfileResource.Name`)
- Trash ID (from `QualityProfileResource.TrashId`)
- Qualities (derived from `Items` array where `allowed: true`)

### Intentionally Unused Fields

- `TrashDescription`: Contains HTML (`<br>` tags), verbose, duplicates qualities data
- `TrashScoreSet`, `Language`: Not primary info for listing

### PDR Required

A Product Decision Record is needed to document why `trash_description` is unused. Rationale:

- Guide maintainers expect their data to be consumed
- Need documented answer for "why isn't my description showing?"
- Establishes precedent for upstream data contract decisions

This also requires updating `docs/CLAUDE.md` to add guidance about documenting upstream data
contract deviations via PDRs.

## UI Mockup Options Presented

### Option A: Simple Table

```txt
| Name                | Trash ID                         |
|---------------------|----------------------------------|
| [Anime] Remux-1080p | 20e0fc959f1f1704bed501f23bdae76f |
| HD Bluray + WEB     | ed38b889c31b4e0e9a6cf2db74c73b5c |
```

Qualities omitted (too wide).

### Option B: Table with Qualities Column

Three columns but may wrap awkwardly on narrow terminals.

### Option C: Tree Structure

```txt
├── [Anime] Remux-1080p
│   ├── Trash ID: 20e0fc959f1f1704bed501f23bdae76f
│   └── Qualities: SDTV, DVD, WEB 480p, ...
├── HD Bluray + WEB
│   ├── Trash ID: ed38b889c31b4e0e9a6cf2db74c73b5c
│   └── Qualities: Bluray-720p, Bluray|WEB 1080p
```

### Option D: Rows with Labels (Recommended)

```txt
Quality Profiles in the TRaSH Guides
────────────────────────────────────

[Anime] Remux-1080p
  Trash ID:   20e0fc959f1f1704bed501f23bdae76f
  Qualities:  SDTV, DVD, WEB 480p, Bluray-480p, WEB 720p, Bluray-720p,
              WEB 1080p, Bluray 1080p

HD Bluray + WEB
  Trash ID:   ed38b889c31b4e0e9a6cf2db74c73b5c
  Qualities:  Bluray-720p, Bluray|WEB 1080p
```

Clean, scannable, handles long quality lists gracefully.

### Option E: Panels per Profile

Each profile in a bordered box. More visual separation but cluttered.

## Raw Output Format

TSV: `{trash_id}\t{name}\t{qualities_csv}`

## Implementation Files

- Create: `src/Recyclarr.Cli/Console/Commands/ListQualityProfilesCommand.cs`
- Edit: `src/Recyclarr.Cli/Console/CliSetup.cs` (register command)
- Create: `docs/decisions/product/005-quality-profile-description-unused.md`
- Edit: `docs/CLAUDE.md` (upstream contract guidance)

## Data Source

`QualityProfileResourceQuery.Get(SupportedServices)` - already registered in DI.

## Open Items

- User to select UI style (A-E)
- URL linking deferred pending TRaSH Guides maintainer sign-off on adding `trash_url` field

## Reference

- Linear: REC-27
- Blocks: REC-44 (documentation)
