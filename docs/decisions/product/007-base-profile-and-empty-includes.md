# PDR-007: Base Profile and empty CF group includes

- **Upstream:** [Discord thread][upstream] in TRaSH Guides third-party sync tools channel

[upstream]: https://discord.com/channels/492590071455940612/833801961043394660/1498324953349492767

## Context

PR #2711 added many new CF groups to TRaSH Guides, along with a "Base Profile" in a new "TEST"
quality profile group. About 84% of all CF groups now reference this Base Profile in their
`quality_profiles.include` section.

The Base Profile exists because the CF groups JSON schema (`cf-groups.schema.json`) has
`minProperties: 1` on `quality_profiles.include`, which means every group must reference at least
one profile. Groups that aren't tied to any real guide profile (audio channels, optional
resolutions, language-specific filters, etc.) need somewhere to point, so they point at Base
Profile.

The schema was introduced in PR #2628, which was AI-generated. The `minProperties: 1` constraint was
likely a default based on the data at the time (all existing groups already had profile references),
not a deliberate design decision. A TRaSH Guides contributor confirmed the Base Profile was created
specifically because "I can't add a group without assigning it to a profile." The contributor's GUI
authoring tool enforces the same constraint.

The contributor explained the schema rule is a safeguard against missing or wrong hashes in profile
references, which had caused issues before. The concern is valid, but the solution puts the
workaround in the permanent guide data rather than in the authoring/review process.

Recyclarr's view is that catching missing hashes belongs in the PR and merge process, not in the
data. The Base Profile ripples downstream: `config-templates` needs
`EXCLUDED_PROFILE_GROUPS = {"TEST"}` in its generation script, every sync tool reading
`quality-profile-groups/groups.json` must decide what to do with a group not meant for syncing, and
the raw JSON contains a profile described as "a base profile that we use for testing." The
contributor did not respond to the process alternative.

## Decision

Recyclarr does not special-case the Base Profile or the TEST profile group. Auto-sync matches
groups to user-configured profiles strictly by trash_id, and no user configures the Base Profile,
so its references are ignored at runtime. Recyclarr accepts an empty `quality_profiles.include`:
such a group never auto-syncs but can still be added with `custom_format_groups.add`.

Pursue removal of the schema constraint upstream only if the upstream position changes.

## Rejected Options

- Filter the TEST group inside Recyclarr: adds a special case for data that is already harmless at
  runtime.

## Consequences

- Config: None
- Migration: not required
- `config-templates` keeps its TEST group exclusion. If more internal profile groups appear
  upstream, that list grows; revisit this decision then.

## History

- 2026-04-29: No Recyclarr change for the Base Profile; empty includes remain valid.
