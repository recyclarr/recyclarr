using Recyclarr.Settings.Models;

namespace Recyclarr.Settings.Deprecations;

internal class RepositoriesToResourceProvidersDeprecationCheck(ILogger log)
    : ISettingsDeprecationCheck
{
    public bool CheckIfNeeded(RecyclarrSettings settings)
    {
        return settings.Repositories is not null;
    }

    public RecyclarrSettings Transform(RecyclarrSettings settings)
    {
        if (settings.Repositories is null)
        {
            return settings;
        }

        log.Warning(
            "DEPRECATED: The `repositories` setting is deprecated and will be removed in a future version. "
                + "Please migrate to the new `resource_providers` format. "
                + "See: <https://recyclarr.dev/guide/upgrade-guide/v8.0/#resource-providers>"
        );

        var newProviders = new List<ResourceProvider>(settings.ResourceProviders);

        // Transform trash_guides repository to resource provider
        if (settings.Repositories.TrashGuides.CloneUrl is not null)
        {
            var gitProvider = new GitResourceProvider
            {
                Name = "official",
                Type = "trash-guides",
                CloneUrl = settings.Repositories.TrashGuides.CloneUrl,
                Reference = settings.Repositories.TrashGuides.Branch ?? "master",
                ReplaceDefault = true,
            };
            newProviders.Add(gitProvider);
        }

        // Transform config_templates repository to resource provider
        if (settings.Repositories.ConfigTemplates.CloneUrl is not null)
        {
            var gitProvider = new GitResourceProvider
            {
                Name = "official",
                Type = "config-templates",
                CloneUrl = settings.Repositories.ConfigTemplates.CloneUrl,
                Reference = settings.Repositories.ConfigTemplates.Branch ?? "master",
                ReplaceDefault = true,
            };
            newProviders.Add(gitProvider);
        }

        return settings with
        {
            Repositories = null, // Clear deprecated field after transformation
            ResourceProviders = newProviders,
        };
    }
}
