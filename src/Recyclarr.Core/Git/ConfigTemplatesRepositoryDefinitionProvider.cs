using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Git;

internal class ConfigTemplatesRepositoryDefinitionProvider(
    ISettings<ResourceProviderSettings> settings
) : IRepositoryDefinitionProvider
{
    public string RepositoryType => "config-templates";

    public IEnumerable<GitRepositorySource> GetRepositoryDefinitions()
    {
        // Always include official config templates repository first
        var officialRepo = new GitRepositorySource
        {
            Name = "official",
            CloneUrl = new Uri("https://github.com/recyclarr/config-templates.git"),
            Reference = "master",
        };

        return new[] { officialRepo }.Concat(
            settings.Value.ConfigTemplates.OfType<GitRepositorySource>()
        );
    }
}
