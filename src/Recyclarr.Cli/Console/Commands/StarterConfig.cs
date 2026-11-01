namespace Recyclarr.Cli.Console.Commands;

/// <summary>What <c>config create</c> writes when no template is given.</summary>
internal static class StarterConfig
{
    // Content is indented four spaces so the schema line fits the source line limit.
    public const string Yaml = """
        # yaml-language-server: $schema=https://schemas.recyclarr.dev/latest/config-schema.json

        # An empty starter config to use with Recyclarr. Update the values below as needed for your
        # instance. You will be required to update the `api_key` and `base_url` for each instance
        # you want to use.
        #
        # If you'd rather start from a configuration built from a TRaSH Guides quality profile, run
        # `recyclarr config list templates` and then `recyclarr config create -t <template>`.
        # See: https://recyclarr.dev/guide/guide-configs/
        #
        # This file WILL NOT WORK as it currently is. You need to read the documentation and build
        # this configuration from scratch. Note that there's no "one size fits all" configuration.
        # Please refer to the TRaSH Guides to understand how to build the appropriate configuration
        # based on your hardware setup and capabilities.
        #
        # For more details on the configuration, see the Configuration Reference on the wiki here:
        # https://recyclarr.dev/reference/configuration/
        #
        # Want a more flexible file layout?
        # See: https://recyclarr.dev/guide/file-structure/

        # Configuration specific to Sonarr. For Radarr, the layout is the same.
        # See: https://recyclarr.dev/reference/configuration/basic/
        sonarr:
          series:
            # Set the URL/API Key to your actual instance
            base_url: http://localhost:8989
            api_key: YOUR_KEY_HERE

            # See: https://recyclarr.dev/reference/configuration/quality-definition/
            quality_definition:

            # See: https://recyclarr.dev/reference/configuration/quality-profiles/
            quality_profiles:

            # See: https://recyclarr.dev/reference/configuration/custom-formats/
            custom_formats:

            # See: https://recyclarr.dev/reference/configuration/media-naming/
            media_naming:

        """;
}
