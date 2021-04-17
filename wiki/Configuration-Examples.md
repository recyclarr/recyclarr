Various scenarios supported using the flexible configuration support.

## Update as much as possible in both Sonarr and Radarr with a single config

Create a single configuration file (use the default `trash.yml` if you want to simplify your CLI
usage by not being required to specify `--config`) and put all of the configuration in there, like
this:

```yml
sonarr:
  - base_url: http://localhost:8989
    api_key: f7e74ba6c80046e39e076a27af5a8444
    quality_definition: hybrid
    release_profiles:
      - type: anime
        strict_negative_scores: true
        tags:
          - anime
      - type: series
        strict_negative_scores: false
        tags:
          - tv

radarr:
  - base_url: http://localhost:7878
    api_key: bf99da49d0b0488ea34e4464aa63a0e5
    quality_definition:
      type: movie
      preferred_ratio: 0.5
```

Even though it's all in one file, Radarr settings are ignored when you run `trash sonarr` and vice
versa. To update both, just chain them together in your terminal, like so:

```bash
trash sonarr && trash radarr
```

This scenario is pretty ideal for a cron job you have running regularly and you want it to update
everything possible in one go.

## Selectively update different parts of Sonarr

Say you want to update Sonarr release profiles from the guide, but not the quality definitions.
There's no command line option to control this, so how do you do it?

Simply create two YAML files:

`sonarr-release-profiles.yml`:

```yml
sonarr:
  - base_url: http://localhost:8989
    api_key: f7e74ba6c80046e39e076a27af5a8444
    release_profiles:
      - type: anime
        tags:
          - anime
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
trash sonarr --config sonarr-release-profiles.yml
```

This will only update release profiles since you have essentially moved the `quality_definition`
property to its own file. When you want to update both, you just specify both files the next time
you run the program:

```bash
trash sonarr --config sonarr-release-profiles.yml sonarr-quality-definition.yml
```
