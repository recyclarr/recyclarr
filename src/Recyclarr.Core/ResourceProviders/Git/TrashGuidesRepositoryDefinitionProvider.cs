using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Git;

internal class TrashGuidesRepositoryDefinitionProvider(ISettings<ResourceProviderSettings> settings)
    : BaseRepositoryDefinitionProvider
{
    public override string RepositoryType => "trash-guides";

    protected override IReadOnlyCollection<IUnderlyingResourceProvider> GetUserRepositories()
    {
        return settings.Value.TrashGuides;
    }

    protected override GitRepositorySource CreateOfficialRepository()
    {
        return new GitRepositorySource
        {
            Name = "official",
            CloneUrl = new Uri("https://github.com/TRaSH-Guides/Guides.git"),
            Reference = "master",
        };
    }
}
