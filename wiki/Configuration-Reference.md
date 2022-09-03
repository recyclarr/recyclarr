Reference documentation for the YAML configuration. For various examples, see the [Configuration
Examples] page.

[Configuration Examples]: https://github.com/recyclarr/recyclarr/wiki/Configuration-Examples

# Summary

The Recyclarr program utilizes YAML for its configuration files. The configuration can be set up
multiple ways, offering a lot of flexibility:

- You may use one or more YAML files simultaneously, allowing you to divide your configuration
  properties up in such a way that you can control what gets updated based on which files you
  specify.

- Each YAML file may have one or more service configurations. This means you can have one file
  define settings for just Sonarr, Radarr, or both services. The program will only read the
  configuration from the file relevant for the specific service subcommand you specified (e.g.
  `recyclarr sonarr` will only read the Sonarr config in the file, even if Radarr config is
  present).

> **Remember**: If you do not specify the `--config` argument, the program will look for
> `recyclarr.yml` in the [application data directory][FileStructure].

[FileStructure]: https://github.com/recyclarr/recyclarr/wiki/File-Structure

# YAML Reference

Table of Contents

- [All Services](#all-services)
  - [Basic Settings](#basic-settings)
  - [Custom Format Settings](#custom-format-settings)
- [Sonarr](#sonarr)
  - [Quality Definition Settings](#quality-definition-settings)
  - [Release Profile Settings](#release-profile-settings)
- [Radarr](#radarr)
  - [Quality Definition Settings](#quality-definition-settings-1)

## All Services

The below settings are applicable to both Sonarr and Radarr.

### Basic Settings

- `base_url` **(Required)**<br>
  The base URL of your instance. Basically this is the URL you bookmark to get to the front page.

- `api_key` **(Required)**<br>
  The API key that Recyclarr should use to synchronize settings to your instance. You can obtain
  your API key by going to `Settings > General` and copy & paste the "API Key" under the "Security"
  group/header.

### Custom Format Settings

For details on the way Custom Formats are synchronized, visit the [[Custom Format Synchronization]]
page.

- `delete_old_custom_formats` (Optional; *Default: `false`*)<br>
  If enabled, custom formats that you remove from your YAML configuration OR that are removed from
  the guide will be deleted from your Radarr instance. Note that this *only* applies to custom
  formats that Recyclarr has synchronized to Radarr. Custom formats that you have added manually in
  Radarr **will not be deleted** if you enable this setting.

- `custom_formats` (Optional; *Default: No custom formats are synced*)<br>
  A list of one or more sets of custom formats each with an optional set of quality profiles names
  that identify which quality profiles to assign the scores for those custom formats to. The child
  properties documented below apply to each element of this list.

  - `trash_ids` (Optional; *`names` is required if not used*)<br>
    A list of one or more Trash IDs of custom formats to synchronize to Radarr. The IDs *must* be
    taken from the value of the `"trash_id"` property in the JSON itself. It will look like the
    following:

    ```json
    {
      "trash_id": "496f355514737f7d83bf7aa4d24f8169",
    }
    ```

    **TIP:** To ease the readability concerns of using IDs instead of names, leave a comment beside
    the Trash ID in your configuration so it can be easily identified later. For example:

    ```yml
    trash_ids:
      - 5d96ce331b98e077abb8ceb60553aa16 # dovi
      - a570d4a0e56a2874b64e5bfa55202a1b # flac
    ```

    > **A Few Things to Remember**
    >
    > - If `delete_old_custom_formats` is set to true, custom formats are **deleted** in Radarr if
    >   you remove them from this list.
    > - It's OK for the same custom format to exist in multiple lists of `trash_ids`. Recyclarr will
    >   only ever synchronize it once. Allowing it to be specified multiple times allows you to
    >   assign it to different profiles with different scores.

  - `quality_profiles` (Optional; *Default: No quality profiles are changed*)<br>
    One or more quality profiles to update with the scores from the custom formats listed above.
    Scores are taken from the guide by default, with an option to override the score for all of
    them. Each object in the list must use the properties below.

    - `name` **(Required)**<br>
      The name of one of the quality profiles in Radarr.

    - `score` (Optional; *Default: Use scores from the guide*)<br>
      A positive or negative number representing the score to apply to *all* custom formats listed
      in the `names` list. A score of `0` is also acceptable, which effectively disables the custom
      formats without having to delete them.

    - `reset_unmatched_scores` (Optional; *Default: `false`*)<br>
      If set to `true`, enables setting scores to `0` in quality profiles where either a name was
      not mentioned in the `names` array *or* it was in that list but did not get a score (e.g. no
      score in guide). If `false`, scores are never altered unless it is listed in the `names` array
      *and* has a valid score to assign.

## Sonarr

```yml
sonarr:
  - base_url: http://localhost:8989
    api_key: f7e74ba6c80046e39e076a27af5a8444

    # Quality Definition Settings
    quality_definition: hybrid

    # Release Profile Settings
    release_profiles:
      - trash_ids:
          - d428eda85af1df8904b4bbe4fc2f537c # Anime - First release profile
          - 6cd9e10bb5bb4c63d2d7cd3279924c7b # Anime - Second release profile
        strict_negative_scores: true
        tags: [anime]
      - trash_ids:
          - EBC725268D687D588A20CBC5F97E538B # Low Quality Groups
          - 1B018E0C53EC825085DD911102E2CA36 # Release Sources (Streaming Service)
          - 71899E6C303A07AF0E4746EFF9873532 # P2P Groups + Repack/Proper
        strict_negative_scores: false
        tags: [tv]
      - trash_ids: [76e060895c5b8a765c310933da0a5357] # Optionals
        filter:
          include:
            - 436f5a7d08fbf02ba25cb5e5dfe98e55 # Ignore Dolby Vision without HDR10 fallback
            - f3f0f3691c6a1988d4a02963e69d11f2 # Ignore The Group -SCENE
        tags: [tv]
```

### Quality Definition Settings

- `quality_definition` (Optional)<br>
  A quality definition type found by running the `recyclarr sonarr --list-qualities` command that
  identifies the quality size settings that should be parsed and uploaded to Sonarr.

  There's one special case type here that won't appear in the output of the above command, nor is it
  one that exists in the guide: `hybrid`. It is a combination of both the `anime` and `series`
  quality definitions that is calculated by comparing each quality and taking both the smallest
  minimum and largest maximum values. The purpose of the `hybrid` type is to build the most
  permissive quality definition that the guide will allow. It's a good idea to use this one if you
  want more releases to be blocked by your release profiles instead of quality.

### Release Profile Settings

- `release_profiles` (Optional; *Default: No release profiles are synced*)<br>
  A list of release profiles to parse from the guide. Each object in this list supports the below
  properties.

  - `trash_ids` **(Required)**<br>
    A list of one or more Trash IDs taken from [the Trash Guide Sonarr JSON files][sonarrjson].

  - `strict_negative_scores` (Optional; *Default: `false`*)<br>
    Enables preferred term scores less than 0 to be instead treated as "Must Not Contain" (ignored)
    terms. For example, if something is "Preferred" with a score of `-10`, it will instead be put in
    the "Must Not Contains" section of the uploaded release profile. Must be `true` or `false`.

  - `tags` (Optional; *Default: Empty list*)<br>
    A list of one or more strings representing tags that will be applied to this release profile.
    Tags are created in Sonarr if they do not exist. All tags on an existing release profile (if
    present) are removed and replaced with only the tags in this list. If no tags are specified, no
    tags will be set on the release profile.

  - `filter` (Optional)<br>
    Defines various ways that release profile terms from the guide are synchronized with Sonarr. Any
    filters below that takes a list of `trash_id` values, those values come, again, from the [Sonarr
    JSON Files][sonarrjson]. There is a `trash_id` field next to each `term` field; that is what you
    use.

    - `include`<br>
      A list of `trash_id` values representing terms (Required, Ignored, or Preferred) that should
      be included in the created Release Profile in Sonarr. Terms that are NOT specified here are
      excluded automatically. Not compatible with `exclude` and will take precedence over it.

    - `exclude`<br>
      A list of `trash_id` values representing terms (Required, Ignored, or Preferred) that should
      be excluded from the created Release Profile in Sonarr. Terms that are NOT specified here are
      included automatically. Not compatible with `include`; this list is not used if it is present.

[sonarrjson]: https://github.com/TRaSH-/Guides/tree/master/docs/json/sonarr

## Radarr

```yml
radarr:
  - base_url: http://localhost:7878
    api_key: bf99da49d0b0488ea34e4464aa63a0e5

    # Quality Definition Settings
    quality_definition:
      type: movie
      preferred_ratio: 0.5

    # Custom Format Settings
    delete_old_custom_formats: false
    custom_formats:
      - trash_ids:
        - ed38b889b31be83fda192888e2286d83 #BR-DISK
        - 90cedc1fea7ea5d11298bebd3d1d3223 #EVO (no WEBDL)
        - 90a6f9a284dff5103f6346090e6280c8 #LQ
        - dc98083864ea246d05a42df0d05f81cc #x265 (720/1080p)
        - b8cd450cbfa689c0259a01d9e29ba3d6 #3D
        - ae9b7c9ebde1f3bd336a8cbd1ec4c5e5 #No-RlsGroup
        - 7357cf5161efbf8c4d5d0c30b4815ee2 #Obfuscated
        - 5c44f52a8714fdd79bb4d98e2673be1f #Retags
        - b6832f586342ef70d9c128d40c07b872 #Bad Dual Groups
        - 923b6abef9b17f937fab56cfcf89e1f1 #DV (WEBDL)
        quality_profiles:
          - name: HD-1080p
          - name: HD-720p2
            score: -1000
      - trash_ids:
          - 496f355514737f7d83bf7aa4d24f8169 #TrueHD ATMOS
          - 2f22d89048b01681dde8afe203bf2e95 #DTS X
        quality_profiles:
          - name: SD
```

### Quality Definition Settings

- `quality_definition` (Optional)<br>
  Specify information related to Radarr quality definition processing here. Only the following child
  properties are permitted. If not specified, no quality definitions will be synced.

  - `type` **(Required)**<br>
    A quality definition type found by running the `recyclarr radarr --list-qualities` command that
    identifies the quality size settings that should be parsed and uploaded to Radarr.

  - `preferred_ratio` (Optional; *Default: `1.0`*)<br>
    A value `0.0` to `1.0` that represents the percentage (interpolated) position of that middle
    slider you see when you enable advanced settings on the Quality Definitions page in Radarr. A
    value of `0.0` means the preferred quality will match the minimum quality. Likewise, `1.0` will
    match the maximum quality. A value such as `0.5` will keep it halfway between the two.

    Any value less than `0` or greater than `1` will result in a warning log printed and the value
    will be clamped.
