Reference documentation for the YAML documentation. For various configuration examples, see the
[[Configuration Examples]] page.

## Summary

The Trash Updater program utilizes YAML for its configuration files. The configuration can be set up
multiple ways, offering a lot of flexibility:

- You may use one or more YAML files simultaneously, allowing you to divide your configuration
  properties up in such a way that you can control what gets updated based on which files you
  specify.
- Each YAML file may have one or more service configurations. This means you can have one file
  define settings for just Sonarr, Radarr, or both services. The program will only read the
  configuration from the file relevant for the specific service subcommand you specified (e.g.
  `trash sonarr` will only read the Sonarr config in the file, even if Radarr config is present)

> **Remember**: If you do not specify the `--config` argument, the program will look for `trash.yml`
> in the same directory where the executable lives.

## YAML Reference

### Sonarr

```yml
sonarr:
  - base_url: http://localhost:8989
    api_key: f7e74ba6c80046e39e076a27af5a8444

    # Quality definitions from the guide to sync to Sonarr.
    quality_definition: hybrid

    # Release profiles from the guide to sync to Sonarr.
    release_profiles:
      - type: anime
        strict_negative_scores: true
        tags:
          - anime
      - type: series
        strict_negative_scores: false
        filter:
          include_optional: true
        tags:
          - tv
```

- `base_url` (Required)<br>
  The base URL of your Sonarr instance. Basically this is the URL you bookmark to get to the front
  page.

- `api_key` (Required)<br>
  The API key that Trash Updater should use to synchronize settings to your instance. You can obtain
  your API key by going to `Sonarr > Settings > General` and copy & paste the "API Key" under the
  "Security" group/header.

- `quality_definition` (Optional)<br>
  The quality definition [from the TRaSH Guide's Quality Settings page][sonarr_quality] that should
  be parsed and uploaded to Sonarr. Only the below values are permitted here.

  - `anime`: Represents the "Sonarr Quality Definitions" table specifically for Anime
  - `series`: Represents the "Sonarr Quality Definitions" table intended for normal TV Series.
    Sometimes referred to as non-anime.
  - `hybrid`: A combination of both the `anime` and `series` tables that is calculated by comparing
    each row and taking both the smallest minimum and largest maximum values. The purpose of the
    Hybrid type is to build the most permissive quality definition that the guide will allow. It's a
    good idea to use this one if you want more releases to be blocked by your release profiles
    instead of quality.

- `release_profiles` (Optional)<br>
  A list of release profiles to parse from the guide. Each object in this list supports the below
  properties.

  - `type` (Required): Must be one of the following values:
    - `anime`: Parse the [Anime Release Profile][sonarr_profile_anime] page from the TRaSH Guide.
    - `series`: Parse the [WEB-DL Release Profile][sonarr_profile_series] page from the TRaSH Guide.

  - `strict_negative_scores` (Optional): Enables preferred term scores less than 0 to be instead
    treated as "Must Not Contain" (ignored) terms. For example, if something is "Preferred" with a
    score of `-10`, it will instead be put in the "Must Not Contains" section of the uploaded
    release profile. Must be `true` or `false`. The default value is `false` if omitted.

  - `tags` (Optional): A list of one or more strings representing tags that will be applied to this
    release profile. Tags are created in Sonarr if they do not exist. All tags on an existing
    release profile (if present) are removed and replaced with only the tags in this list. If no
    tags are specified, no tags will be set on the release profile.

  - `filter` (Optional): Defines various ways that release profile terms from the guide are
    synchronized with Sonarr. Any combination of the below properties may be specified here:
    - `include_optional`: Set to `true` to include terms marked "Optional" in the guide. By default,
      optional terms are *not* synchronized to Sonarr. The default is `false`.

[sonarr_quality]: https://trash-guides.info/Sonarr/V3/Sonarr-Quality-Settings-File-Size/
[sonarr_profile_anime]: https://trash-guides.info/Sonarr/V3/Sonarr-Release-Profile-RegEx-Anime/
[sonarr_profile_series]: https://trash-guides.info/Sonarr/V3/Sonarr-Release-Profile-RegEx/

### Radarr

```yml
radarr:
  - base_url: http://localhost:7878
    api_key: bf99da49d0b0488ea34e4464aa63a0e5

    # Which quality definition in the guide to sync to Radarr.
    quality_definition:
      type: movie
      preferred_ratio: 0.5
```

- `base_url` (Required)<br>
  The base URL of your Radarr instance. Basically this is the URL you bookmark to get to the front
  page.

- `api_key` (Required)<br>
  The API key that Trash Updater should use to synchronize settings to your instance. You can obtain
  your API key by going to `Radarr > Settings > General` and copy & paste the "API Key" under the
  "Security" group/header.

- `quality_definition` (Optional)<br>
  Specify information related to Radarr quality definition processing here. Only the following child
  properties are permitted.

  - `type` (Required): The quality definition from the [Radarr Quality Settings (File
    Size)][radarr_quality] page in the TRaSH Guides that should be parsed and uploaded to Radarr.
    Only the below values are permitted here.
    - `movie`: Currently the only supported type. Represents the only table on that page and is
      intended for general use with all movies in Radarr.

  - `preferred_ratio` (Optional) A value `0.0` to `1.0` that represents the percentage
    (interpolated) position of that middle slider you see when you enable advanced settings on the
    Quality Definitions page in Radarr. A value of `0.0` means the preferred quality will match the
    minimum quality. Likewise, `1.0` will match the maximum quality. A value such as `0.5` will keep
    it halfway between the two.

    If not specified, the default value is `1.0`. Any value less than `0` or greater than `1` will
    result in a warning log printed and the value will be clamped.

[radarr_quality]: https://trash-guides.info/Radarr/V3/Radarr-Quality-Settings-File-Size/
