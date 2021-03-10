With the introduction of version 1.0 of Trash Updater, I am leaving the old Python script behind. I
decided to rewrite the entire application in C# .NET mainly for two reasons:

1. I prefer using and am more comfortable with C#
1. The application started becoming too large and complicated for Python, in my humble opinion.

The rewritten version isn't completely identical to the Python script, unfortunately. The purpose of
this page is to document all of the differences so you can learn the new command line and migrate
your configuration over.

## Command Line Differences

The biggest differences are:

- Nearly all the old CLI options are gone. You no longer have the option of providing something on
  the command line *or* in the YAML config. Everything must be put in the YAML configuration now!
  See [[Configuration Reference]] for details.

- The subcommands are different. Instead of specifying `profile` or `guide` now, you instead mention
  the service you're using, such as `radarr` or `sonarr`. See [[Command Line Reference]] for
  details.

## Configuration Differences

The YAML structure is mostly identical. I recommend you head over to the [[Configuration Reference]]
page and get familiar with the whole schema. But I'll point out a few differences to look out for
here.

### Sonarr

Changed:

- Everything under the top-level `sonarr:` property is now in a list. That means just make the first
  line prefixed with a `-`. This is the list format in YAML. There are actual examples in the
  reference linked above.
- `profile` is now `release_profile`
- `base_uri` is now `base_url` (the `i` at the end became an `L`)

Added:

- Property named `strict_negative_scores` has been added to the `release_profile` objects (since
  it's no longer specified via CLI).
- `quality_definition` has been added under `sonarr`.

### Radarr

Changed:

- Everything under the top-level `radarr:` property is now in a list. That means just make the first
  line prefixed with a `-`. This is the list format in YAML. There are actual examples in the
  reference linked above.

Added:

- `quality_definition` has been added under `radarr`.
