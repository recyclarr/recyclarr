Various scenarios supported using flexible configuration structure:

- [Update as much as possible in both Sonarr and Radarr with a single config](#update-as-much-as-possible-in-both-sonarr-and-radarr-with-a-single-config)
- [Selectively update different parts of Sonarr v3](#selectively-update-different-parts-of-sonarr-v3)
- [Update Sonarr v3 and v4 instances in a single YAML config](#update-sonarr-v3-and-v4-instances-in-a-single-yaml-config)
- [Synchronize a lot of custom formats for a single quality profile](#synchronize-a-lot-of-custom-formats-for-a-single-quality-profile)
- [Manually assign different scores to multiple custom formats](#manually-assign-different-scores-to-multiple-custom-formats)
- [Assign custom format scores the same way to multiple quality profiles](#assign-custom-format-scores-the-same-way-to-multiple-quality-profiles)
- [Scores in a quality profile should be set to zero if it wasn't listed in config](#scores-in-a-quality-profile-should-be-set-to-zero-if-it-wasnt-listed-in-config)

## Update as much as possible in both Sonarr and Radarr with a single config

Create a single configuration file (use the default `recyclarr.yml` if you want to simplify your CLI
usage by not being required to specify `--config`) and put all of the configuration in there, like
this:

```yml
sonarr:
  - base_url: http://localhost:8989
    api_key: f7e74ba6c80046e39e076a27af5a8444
    quality_definition: hybrid
    release_profiles:
      - trash_ids:
          - EBC725268D687D588A20CBC5F97E538B # Low Quality Groups
          - 1B018E0C53EC825085DD911102E2CA36 # Release Sources (Streaming Service)
          - 71899E6C303A07AF0E4746EFF9873532 # P2P Groups + Repack/Proper
        strict_negative_scores: false
        tags: [tv]

radarr:
  - base_url: http://localhost:7878
    api_key: bf99da49d0b0488ea34e4464aa63a0e5
    quality_definition:
      type: movie
      preferred_ratio: 0.5
```

Even though it's all in one file, Radarr settings are ignored when you run `recyclarr sonarr` and
vice versa. To update both, just chain them together in your terminal, like so:

```bash
recyclarr sonarr && recyclarr radarr
```

This scenario is pretty ideal for a cron job you have running regularly and you want it to update
everything possible in one go.

## Selectively update different parts of Sonarr v3

***NOTE:** The official Docker container does not support multiple configuration files*

Say you want to update Sonarr release profiles from the guide, but not the quality definitions.
There's no command line option to control this, so how do you do it?

Simply create two YAML files:

`sonarr-release-profiles.yml`:

```yml
sonarr:
  - base_url: http://localhost:8989
    api_key: f7e74ba6c80046e39e076a27af5a8444
    release_profiles:
      - trash_ids:
          - d428eda85af1df8904b4bbe4fc2f537c # Anime - First release profile
          - 6cd9e10bb5bb4c63d2d7cd3279924c7b # Anime - Second release profile
        tags: [anime]
```

`sonarr-quality-definition.yml`:

```yml
sonarr:
  - base_url: http://localhost:8989
    api_key: f7e74ba6c80046e39e076a27af5a8444
    quality_definition: hybrid
```

Then run the following command:

```bash
recyclarr sonarr --config sonarr-release-profiles.yml
```

This will only update release profiles since you have essentially moved the `quality_definition`
property to its own file. When you want to update both, you just specify both files the next time
you run the program:

```bash
recyclarr sonarr --config sonarr-release-profiles.yml sonarr-quality-definition.yml
```

## Update Sonarr v3 and v4 instances in a single YAML config

If you have two instances of Sonarr, one v3 and one v4, that you'd like to update from a single run
using one YAML file, you can do that by simply specifying both in the list under the `sonarr`
property:

```yml
sonarr:
  - base_url: http://sonarr_v4:8989
    api_key: f7e74ba6c80046e39e076a27af5a8444
    quality_definition: anime
    custom_formats:
      - trash_ids:
          - 949c16fe0a8147f50ba82cc2df9411c9 # Anime BD Tier 01 (Top SeaDex Muxers)
          - ed7f1e315e000aef424a58517fa48727 # Anime BD Tier 02 (SeaDex Muxers)
          - 096e406c92baa713da4a72d88030b815 # Anime BD Tier 03 (SeaDex Muxers)
        quality_profiles:
          - name: Anime Subs
  - base_url: http://sonarr_v3:8989
    api_key: bf99da49d0b0488ea34e4464aa63a0e5
    quality_definition: series
    release_profiles:
      - trash_ids:
          - EBC725268D687D588A20CBC5F97E538B # Low Quality Groups
          - 1B018E0C53EC825085DD911102E2CA36 # Release Sources (Streaming Service)
          - 71899E6C303A07AF0E4746EFF9873532 # P2P Groups + Repack/Proper
```

In the example above, two separate instances, each with its own API key, will be updated. One
instance is for Anime only. The other is for Series (TV) only. And since I'm using two instances, I
don't bother with tags, so I am able to leave those elements out. Recyclarr knows when it's talking
to either a v3 or v4 instance of Sonarr and will correctly anticipate either `release_profiles` or
`custom_formats` (respectively).

When you run `recyclarr sonarr` (specify `--config` if you aren't using the default `recyclarr.yml`)
it will update both instances.

## Synchronize a lot of custom formats for a single quality profile

Scenario: *"I want to be able to synchronize a list of custom formats to Radarr. In addition, I want
the scores in the guide to be applied to a single quality profile."*

Solution:

```yml
radarr:
  - base_url: http://localhost:7878
    api_key: 87674e2c316645ed85696a91a3d41988

    custom_formats:
      # Advanced Audio from the guide
      - trash_ids:
        - 496f355514737f7d83bf7aa4d24f8169 # TrueHD ATMOS
        - 2f22d89048b01681dde8afe203bf2e95 # DTS X
        - 417804f7f2c4308c1f4c5d380d4c4475 # ATMOS (undefined)
        - 1af239278386be2919e1bcee0bde047e # DD+ ATMOS
        - 3cafb66171b47f226146a0770576870f # TrueHD
        - dcf3ec6938fa32445f590a4da84256cd # DTS-HD MA
        - a570d4a0e56a2874b64e5bfa55202a1b # FLAC
        - e7c2fcae07cbada050a0af3357491d7b # PCM
        - 8e109e50e0a0b83a5098b056e13bf6db # DTS-HD HRA
        - 185f1dd7264c4562b9022d963ac37424 # DD+
        - f9f847ac70a0af62ea4a08280b859636 # DTS-ES
        - 1c1a4c5e823891c75bc50380a6866f73 # DTS
        - 240770601cc226190c367ef59aba7463 # AAC
        - c2998bd0d90ed5621d8df281e839436e # DD
        quality_profiles:
          - name: SD
```

## Manually assign different scores to multiple custom formats

Scenario: *"I want to synchronize custom formats to Radarr or Sonarr. I also do not want to use the
scores in the guide for some of the CFs. Instead, I want to assign my own distinct score to some
custom formats in a single quality profile."*

Solution:

```yml
radarr:
  - base_url: http://localhost:7878
    api_key: 87674e2c316645ed85696a91a3d41988

    custom_formats:
      # Take scores in the guide for these 3
      - trash_ids:
          - 3cafb66171b47f226146a0770576870f # TrueHD
          - dcf3ec6938fa32445f590a4da84256cd # DTS-HD MA
          - a570d4a0e56a2874b64e5bfa55202a1b # FLAC
        quality_profiles:
          - name: SD

      # Assign manual scores to the 3 below CFs, each added to the same profile
      - trash_ids: [496f355514737f7d83bf7aa4d24f8169] # TrueHD ATMOS
        quality_profiles:
          - name: SD
            score: 100
      - trash_ids: [2f22d89048b01681dde8afe203bf2e95] # DTS X
        quality_profiles:
          - name: SD
            score: 200
      - trash_ids: [417804f7f2c4308c1f4c5d380d4c4475] # ATMOS (undefined)
        quality_profiles:
          - name: SD
            score: 300
```

The configuration is structured around assigning multiple custom formats the same way to just a few
quality profiles. It starts to look more redundant and ugly when you want fine-grained control over
the scores, especially if its on a per-single-custom-format basis.

## Assign custom format scores the same way to multiple quality profiles

You can assign custom format scores (from the guide) to multiple profiles (all the same way):

```yml
radarr:
  - base_url: http://localhost:7878
    api_key: 87674e2c316645ed85696a91a3d41988

    custom_formats:
      - trash_ids:
          - 496f355514737f7d83bf7aa4d24f8169 # TrueHD ATMOS
          - 2f22d89048b01681dde8afe203bf2e95 # DTS X
          - 417804f7f2c4308c1f4c5d380d4c4475 # ATMOS (undefined)
          - 1af239278386be2919e1bcee0bde047e # DD+ ATMOS
          - 3cafb66171b47f226146a0770576870f # TrueHD
        quality_profiles:
          - name: SD
          - name: Ultra-HD
```

Quality profiles named `HD` and `Ultra-HD` will all receive the same scores for the same custom
formats.

You can also choose to override the score (for all custom formats!) in one profile:

```yml
radarr:
  - base_url: http://localhost:7878
    api_key: 87674e2c316645ed85696a91a3d41988

    custom_formats:
      - trash_ids:
          - 496f355514737f7d83bf7aa4d24f8169 # TrueHD ATMOS
          - 2f22d89048b01681dde8afe203bf2e95 # DTS X
          - 417804f7f2c4308c1f4c5d380d4c4475 # ATMOS (undefined)
          - 1af239278386be2919e1bcee0bde047e # DD+ ATMOS
          - 3cafb66171b47f226146a0770576870f # TrueHD
        quality_profiles:
          - name: SD
            score: 100 # This score is assigned to all 5 CFs in this profile
          - name: Ultra-HD # Still uses scores from the guide
```

## Scores in a quality profile should be set to zero if it wasn't listed in config

Scenario: *"I want only the custom formats I assign to a quality profile to be assigned scores.
Scores for other custom formats, **including those I manually assign**, should be reset back to
zero."*

```yml
radarr:
  - base_url: http://localhost:7878
    api_key: 87674e2c316645ed85696a91a3d41988

    custom_formats:
      - trash_ids:
          - 1c7d7b04b15cc53ea61204bebbcc1ee2 # HQ

      - trash_ids:
          - 2f22d89048b01681dde8afe203bf2e95 # DTS X
          - 3cafb66171b47f226146a0770576870f # TrueHD
        quality_profiles:
          - name: SD
            reset_unmatched_scores: true
          - name: Ultra-HD
```

Let's say you have three custom formats added to Radarr: "DTS X", "TrueHD", and "HD". Since only the
first two are listed in the `trash_ids` array, what happens to "HD"? Since two quality profiles are
specified above, each with a different setting for `reset_unmatched_scores`, the behavior will be
different:

- The `SD` quality profile will always have the score for "HD" set to zero (`0`).
- The `Ultra-HD` quality profile's score for "HD" will never be altered.

The `reset_unmatched_scores` setting basically determines how scores are handled for custom formats
that exist in Radarr but are not in the list of `trash_ids` in config. As shown in the example
above, you set it to `true` which results in unmatched scores being set to `0`, or you can set it to
`false` (or leave it omitted) in which case Recyclarr will not alter the value.

Which one should you use? That depends on how much control you want Recyclarr to have. If you use
Recyclarr to supplement manual changes to your profiles, you probably want it set to `false` so it
doesn't clobber your manual edits. Otherwise, set it to `true` so that scores aren't left over when
you add/remove custom formats from a profile.
