using Recyclarr.Platform;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.ResourceProviders.Git;

internal class TrashGuidesRepositoryDefinitionProvider(
    ISettings<ResourceProviderSettings> settings,
    IAppPaths appPaths
) : BaseRepositoryDefinitionProvider(appPaths)
{
    public override string RepositoryType => "trash-guides";

    protected override IReadOnlyCollection<ResourceProvider> GetUserProviders()
    {
        return settings.Value.Providers;
    }

    protected override GitResourceProvider CreateOfficialRepository()
    {
        return new GitResourceProvider
        {
            Name = "official",
            Type = "trash-guides",
            CloneUrl = new Uri("https://github.com/TRaSH-Guides/Guides.git"),
            Reference = "master",
        };
    }
}
