Custom format synchronization is broken up into three categories:

- Creation: Custom formats that are in the guide but do not exist in Radarr are **created**.
- Updates: Custom formats that already exist in both the guide and in Radarr are **updated**.
- Deletions: If deletions are allowed by having the `delete_old_custom_formats` configuration
  setting set to `true`, then custom formats in Radarr are deleted if they are removed from the
  guide **or** removed from your configuration file.

> **Important**
>
> Recyclarr will *never* touch custom formats that you create by hand, unless they share a name with
> a custom format in the guide. In general, Recyclarr must have been the one to create a custom
> format in order to do anything to it (update or delete).

## Cache

### Summary

The synchronization cache in Recyclarr allows it to more accurately detect changes to custom formats
in the TRaSH guides. This mainly helps cover changes like renames.

Once Recyclarr creates or updates a custom format in Radarr, it records information about it in a
cache file located on disk. The location varies depending on platform:

- Windows: `%APPDATA%/recyclarr/cache`
- Linux: `~/.config/recyclarr/cache`
- MacOS: `~/Library/Application Support/recyclarr/cache`

The cache files are not meant to be edited by users. In general I recommend leaving them alone.
Recyclarr will manage it for you. However, sometimes a bug may cause an issue where deleting the
cache directory will be a good way to recover.

### Custom Format Identification Behavior

The cache stores and remembers the last known valid name for a custom format. If a custom format
gets renamed in the Trash Guide, you don't need to immediately rename it in your YAML config. Trash
Updater will issue a warning in console output when the names become outdated. This gives you plenty
of time to fix the issue.

Note that if the cache gets deleted, custom formats with outdated names will no longer synchronize
to Radarr and you will need to either remove it or fix the name.
