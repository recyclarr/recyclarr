# Resource Providers

## What Are Resource Providers?

Resource providers are how Recyclarr obtains the data it needs to work. Think of them as sources of
information that Recyclarr uses to configure your Radarr and Sonarr instances.

## Key Concepts

- **Resource Collection**: A group of related resources that work together. Recyclarr currently
  supports two collections:
  - **Trash Guides**: Contains custom formats and naming schemes
  - **Config Templates**: Contains configuration templates for easier setup
- **Resource Type**: A category of data within a collection, such as custom formats or media naming
  settings.
- **Resource Item**: A single piece of data, like one specific custom format or one naming template.
- **Resource Provider**: Currently, this is a Git repository that contains the resources Recyclarr
  needs.

## How It Works

1. Recyclarr connects to Git repositories to get the resources it needs
2. Each repository is processed based on its collection type
3. The resources are organized and made available for use in your configuration
4. If any problems occur with one collection, other collections will still work

## Collection Types Explained

### Trash Guides Collection

This collection contains TRaSH Guides resources, which include custom formats and media naming
schemes for both Radarr and Sonarr.

When Recyclarr processes this collection, it:

1. Downloads or updates the repository
2. Finds the guide information in the `metadata.json` file
3. Organizes the custom formats and naming schemes by application (Radarr or Sonarr)

### Config Templates Collection

This collection contains pre-made configuration templates that make it easier to set up Recyclarr.

When Recyclarr processes this collection, it:

1. Downloads or updates the repository
2. Finds templates and reusable template parts (called "includes")
3. Makes these templates available for your configuration

## Setting Up Resource Providers

Below is an example of how to configure resource providers in your settings file:

```yml
# Resource providers configuration
resource_providers:
  # Trash Guides resources
  trash_guides:
    # Official TRaSH Guides repository
    - clone_url: https://github.com/TRaSH-Guides/Guides.git
      name: official
      reference: master

    # You can add your own custom guide repository
    - clone_url: https://github.com/myuser/my-custom-guides.git
      name: my-custom-guides

  # Configuration templates
  config_templates:
    # Official Recyclarr templates
    - clone_url: https://github.com/recyclarr/config-templates.git
      name: official

    # You can add your own custom templates repository
    - clone_url: https://github.com/myuser/my-custom-config-templates.git
      name: my-custom-config-templates
```

## Default Behavior

- You don't need to configure anything to get started - Recyclarr comes with default repositories
- If you want to use different repositories, you can override the defaults by using the name
  "official"
- All other repositories must have unique names
- Ordering is not important
