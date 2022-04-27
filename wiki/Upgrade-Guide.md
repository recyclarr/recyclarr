# Version 2.0

This version introduces changes to the way Sonarr Release Profiles are specified in your YAML
configuration (`trash.yml`). As such, changes are required to your YAML to avoid errors. First,
visit the "Series Types" section to replace the `type` attribute with `trash_ids` as needed. Then
check out the "Term Filters" section to see about removing the `include_optionals` property.

## Series Types

The `type` property under `release_profiles` has been removed. Replaced by a new `trash_ids`
property.

### Drop-In Replacement for Series

For `series`, replace this:

```yml
release_profiles:
  - type: series
```

With this (or you can customize it if you want less):

```yml
release_profiles:
  - trash_ids:
      - EBC725268D687D588A20CBC5F97E538B # Low Quality Groups
      - 1B018E0C53EC825085DD911102E2CA36 # Release Sources (Streaming Service)
      - 71899E6C303A07AF0E4746EFF9873532 # P2P Groups + Repack/Proper
```

### Drop-In Replacement for Anime

For `series`, replace this:

```yml
release_profiles:
  - type: anime
```

With this (or you can customize it if you want less):

```yml
release_profiles:
  - trash_ids:
      - d428eda85af1df8904b4bbe4fc2f537c # Anime - First release profile
      - 6cd9e10bb5bb4c63d2d7cd3279924c7b # Anime - Second release profile
```

## Term Filters

The following changes apply to YAML under the `filter` property.

- Property `include_optional` removed.
- `include` and `exclude` properties added to explicitly choose terms to include or exclude,
  respectively.

### Replacement Examples

If you are coming from YAML like this:

```yml
release_profiles:
  - trash_ids: [EBC725268D687D588A20CBC5F97E538B]
    strict_negative_scores: false
    filter:
      include_optional: true
    tags:
      - tv
```

Simply remove the `include_optional` property above, to get this:

```yml
release_profiles:
  - trash_ids: [EBC725268D687D588A20CBC5F97E538B]
    strict_negative_scores: false
    tags:
      - tv
```

In this release, since you now have the ability to specifically include optionals that you want, I
recommend visiting the [Configuration Reference] and learning more about the `include` and `exclude`
filter lists.

[Configuration Reference]: https://github.com/rcdailey/recyclarr/wiki/Configuration-Reference
