Command line interface documentation for the `Trash` executable.

## Subcommands

Each service (Sonarr, Radarr) has a subcommand that must be specified in order to perform operations
related to that service, such as parsing relevant TRaSH guides and invoking API endpoints to modify
settings on that instance. As always, the `--help` option may be specified following a subcommand to
see more information directly in your terminal.

- `sonarr`: Update release profiles and quality definitions on configured Sonarr instances.
- `radarr`: Update custom formats and quality definitions on configured Radarr instances.

## Common Arguments

These are optional arguments shared by *all* subcommands.

### `--config`

One or more paths to YAML configuration files. Only the relevant configuration section for the
specified subcommand will be read from each file. If this argument is not specified, a single
default configuration file named `trash.yml` will be used. It must be in the same directory as the
`trash` executable.

**Command Line Examples**:

```bash
# Default Config (trash.yml)
trash sonarr

# Single Config
trash sonarr --config ../myconfig.yml

# Multiple Config
trash sonarr --config ../myconfig1.yml "files/my config 2.yml"
```

### `--preview`

Performs a "dry run" by parsing the guide and printing the parsed data in a readable format to the
user. This does *not* perform any API calls to Radarr or Sonarr. You may want to run a preview if
you'd like to see if the guide is parsed correctly before updating your instance.

Example output for Sonarr Release Profile parsing

```txt
First Release Profile
  Include Preferred when Renaming?
    CHECKED

  Must Not Contain:
    /(\[EMBER\]|-EMBER\b|DaddySubs)/i

  Preferred:
    100        /\b(amzn|amazon)\b(?=[ ._-]web[ ._-]?(dl|rip)\b)/i
    90         /\b(dsnp|dsny|disney)\b(?=[ ._-]web[ ._-]?(dl|rip)\b)/i

Second Release Profile
  Include Preferred when Renaming?
    NOT CHECKED

  Preferred:
    180        /(-deflate|-inflate)\b/i
    150        /(-AJP69|-BTN|-CasStudio|-CtrlHD|-KiNGS)\b/i
    150        /(-monkee|-NTb|-NTG|-QOQ|-RTN)\b/i
```

Example output for Sonarr Quality Definition parsing

```txt
Quality              Min        Max
-------              ---        ---
HDTV-720p            2.3        67.5
HDTV-1080p           2.3        137.3
WEBRip-720p          4.3        137.3
WEBDL-720p           4.3        137.3
Bluray-720p          4.3        137.3
WEBRip-1080p         4.5        257.4
WEBDL-1080p          4.3        253.6
Bluray-1080p         4.3        258.1
Bluray-1080p Remux   0          400
HDTV-2160p           69.1       350
WEBRip-2160p         69.1       350
WEBDL-2160p          69.1       350
Bluray-2160p         94.6       400
Bluray-2160p Remux   204.4      400
```

### `--debug`

By default, Info, Warning and Error log levels are displayed in the console. This option enables
Debug level logs to be displayed. This is designed for debugging and development purposes and
generally will be too noisy for normal program usage.
