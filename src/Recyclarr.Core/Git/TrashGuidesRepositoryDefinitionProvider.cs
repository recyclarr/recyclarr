using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Git;

internal class TrashGuidesRepositoryDefinitionProvider(ISettings<ResourceProviderSettings> settings)
    : IRepositoryDefinitionProvider
{
    public string RepositoryType => "trash-guides";

    public IEnumerable<GitRepositorySource> GetRepositoryDefinitions()
    {
        // Always include official TRaSH Guides repository first so it may be overridden
        var officialRepo = new GitRepositorySource
        {
            Name = "official",
            CloneUrl = new Uri("https://github.com/TRaSH-Guides/Guides.git"),
            Reference = "master",
        };

        return new[] { officialRepo }.Concat(
            settings.Value.TrashGuides.OfType<GitRepositorySource>()
        );
    }
}
