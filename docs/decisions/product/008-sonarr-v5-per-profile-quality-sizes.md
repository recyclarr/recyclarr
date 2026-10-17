# PDR-008: Per-profile quality sizes for Sonarr v5

- **Upstream:** [Discord thread][upstream] in TRaSH Guides third-party sync tools channel

[upstream]: https://discord.com/channels/492590071455940612/833801961043394660/1559714084260413512

## Context

Sonarr v5 moved quality size limits (min, preferred, max) from global quality definitions onto the
items of each quality profile. Its upgrade migration copies the existing global sizes into every
profile, after which each profile can diverge. Sonarr v4 keeps global sizes, and both versions are
in use while v5 is in beta.

TRaSH Guides publishes quality size files per type (for example `series` and `anime`) and quality
profiles separately. Guide maintainers agreed that global quality definitions remain, and that a
guide quality profile can point to the quality definition it suggests, which users can override.

The reference format was settled in the same thread.

## Decision

On Sonarr v5, Recyclarr assigns quality sizes per quality profile. For each profile listed under
`quality_profiles`, the first available source wins:

1. The profile's own `quality_definition` in YAML.
2. The quality definition referenced by the guide profile: `trash_quality_definition` in the
   profile JSON, a single `{ "<label>": "<trash_id>" }` entry like `formatItems`. Recyclarr
   matches only the `trash_id`, so renaming files or types cannot break the link; the label keeps
   the JSON readable for maintainers.
3. The instance-level `quality_definition`.

When no source applies, Recyclarr leaves the profile's sizes as they are in Sonarr. Profiles that
are not listed under `quality_profiles`, including profiles only referenced by `assign_scores_to`,
never receive sizes. This follows Recyclarr's rule that it only changes what users configure.

On Sonarr v4 and Radarr, sizes stay global. A profile's YAML `quality_definition` is ignored with
a warning; guide references are ignored silently because users cannot change guide data.

The guide profile JSON change is pending a separate TRaSH Guides PR. The quality size files serve
both global selection and profile references; they need no new flag.

## Rejected Options

- A bare quality size `type` name: breaks when maintainers rename a type.
- A bare `trash_id`: unreadable; guide maintainers have rejected bare IDs before.

## Consequences

- Config: `quality_profiles[].quality_definition`; on Sonarr v5, instance-level
  `quality_definition` applies only to profiles listed under `quality_profiles`
- Migration: not required; existing configs keep working, but on Sonarr v5 sizes no longer reach
  profiles absent from `quality_profiles`
- Users can give anime and series profiles different sizes on the same Sonarr v5 instance.
- A config with `quality_definition` but no `quality_profiles` syncs no sizes on Sonarr v5;
  Recyclarr warns about this.
- On Sonarr v5, profiles that Recyclarr does not manage keep the sizes Sonarr migrated or the user
  set.
- A guide reference with an unknown `trash_id` or more than one entry is a planning error on Sonarr
  v5, and that profile's sizes are skipped, so guide data mistakes surface instead of syncing the
  wrong sizes.

## History

- 2026-10-13: Assign quality sizes per profile on Sonarr v5.
