using Recyclarr.Platform;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Git;

internal class ConfigTemplatesRepositoryDefinitionProvider(
    ISettings<ResourceProviderSettings> settings,
    IAppPaths appPaths
) : BaseRepositoryDefinitionProvider(appPaths)
{
    public override string RepositoryType => "config-templates";

    protected override IReadOnlyCollection<ResourceProvider> GetUserProviders()
    {
        return settings.Value.Providers;
    }

    protected override GitResourceProvider CreateOfficialRepository()
    {
        return new GitResourceProvider
        {
            Name = "official",
            Type = "config-templates",
            CloneUrl = new Uri("https://github.com/recyclarr/config-templates.git"),
            Reference = "master",
        };
    }
}
