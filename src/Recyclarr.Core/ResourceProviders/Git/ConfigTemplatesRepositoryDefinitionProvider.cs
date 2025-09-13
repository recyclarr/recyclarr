using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Git;

internal class ConfigTemplatesRepositoryDefinitionProvider(
    ISettings<ResourceProviderSettings> settings
) : BaseRepositoryDefinitionProvider
{
    public override string RepositoryType => "config-templates";

    protected override IReadOnlyCollection<IUnderlyingResourceProvider> GetUserRepositories()
    {
        return settings.Value.ConfigTemplates;
    }

    protected override GitRepositorySource CreateOfficialRepository()
    {
        return new GitRepositorySource
        {
            Name = "official",
            CloneUrl = new Uri("https://github.com/recyclarr/config-templates.git"),
            Reference = "master",
        };
    }
}
