# Data Sources

## Terminology

- Content Type: A category of data that Recyclarr is interested in. Data sources provide the
  mechanism to access the data for a content type.
- Data Source: Defines how Recyclarr is provided access to specific data types.

## Behavior

General behavior for data source processing:

1. User specifies data sources in settings YAML
2. Recyclarr deserializes the YAML into C# objects
3. Fluent Validation is executed against the data objects to ensure they are valid (e.g. required
   properties are enforced).
4. Data source initialization: Data sources, for each of their content types, are given an
   opportunity to initialize, which would include steps like git clone/update for git repo sources.
   Not all data sources need this, so it's optional.
   - Git sources: Require cloning or updating a repo
5. Obtain paths to resources: The ultimate output of each content type, through its data sources, is
   a list of local paths to one or more types of data (e.g. custom formats, media naming). Each data
   source operates in a unique way to identify the paths to this information.

## Output Expectations

For each content type:

### Trash Guides

- Custom Formats (for sonarr and radarr)
- Media Naming (for sonarr and radarr)

### Config Templates

- Config Templates
- Include Templates

## Settings YAML

```yml
# Configuration for resource providers and their collections
# Each resource collection can have multiple providers that contribute resources
resource_providers:
  # ------------------------------
  # Resource collection: trash guides
  # ------------------------------
  trash_guides:
    # Git-based resource provider (fetches resources from a Git repository)
    - clone_url: https://github.com/TRaSH-Guides/Guides.git # required
      # Unique identifier for this provider within this resource collection
      # 'official' is reserved for official repositories and can be overridden
      # Only alphanumeric characters, dashes, and underscores are permitted
      name: official
      # Optional branch name or sha1 commit hash
      reference: master # could also be `0c0f2a1b8e3d4f5c7a6b9d8e4f5c7a6b9d8e4f5`

    # Git-based resource provider (fetches resources from a Git repository)
    # Custom repositories must include a `metadata.json` file defining resource locations
    - clone_url: https://github.com/myuser/my-custom-guides.git
      name: my-custom-guides

    # Filesystem-based resource provider (fetches resources from a local directory)
    # Useful if you manage a clone of the trash guide repo outside of recyclarr
    - path: /absolute/path/to/local/guides

  # ------------------------------
  # Resource collection: config templates
  # ------------------------------
  config_templates:
    # Git-based resource provider for official config templates
    - clone_url: https://github.com/recyclarr/config-templates.git
      name: official

    # Git-based resource provider for custom config templates
    # Custom template repositories require both `templates.json` and `includes.json` files
    - clone_url: https://github.com/myuser/my-custom-config-templates.git
      name: my-custom-config-templates

  # ------------------------------
  # Resource collection: custom formats
  # ------------------------------
  custom_formats:
    # Application-specific resources - each application can have its own set of providers
    radarr:
      # Filesystem-based resource provider
      - path: /absolute/path/to/radarr/custom/formats

      # Git-based resource provider
      - clone_url: https://github.com/myuser/my-custom-formats.git
        name: my-custom-formats

    # Application-specific resources
    sonarr:
      # Filesystem-based resource provider
      - path: /absolute/path/to/sonarr/custom/formats

  # ------------------------------
  # Resource collection: media naming
  # ------------------------------
  media_naming:
    # Application-specific resources - each application can have its own set of providers
    radarr:
      # Filesystem-based resource provider (relative to config directory)
      - path: relative/to/config/dir

      # Git-based resource provider
      - clone_url: https://github.com/myuser/my-custom-media-naming.git
        name: my-custom-media-naming
```
