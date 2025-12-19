using Recyclarr.Settings.Deprecations;
using Recyclarr.Settings.Models;

namespace Recyclarr.Core.Tests.Settings.Deprecations;

internal sealed class RepositoriesToResourceProvidersDeprecationCheckTest
{
    [Test, AutoMockData]
    public void Transform_converts_repositories_to_resource_providers(
        RepositoriesToResourceProvidersDeprecationCheck sut
    )
    {
        var settings = new RecyclarrSettings
        {
            Repositories = new Repositories
            {
                TrashGuides = new TrashRepository
                {
                    CloneUrl = new Uri("https://github.com/TRaSH-Guides/Guides.git"),
                    Branch = "master",
                },
                ConfigTemplates = new ConfigTemplateRepository
                {
                    CloneUrl = new Uri("https://github.com/recyclarr/config-templates.git"),
                    Branch = "main",
                },
            },
        };

        var result = sut.Transform(settings);

        result.Repositories.Should().BeNull();

        // Should create new resource providers
        result
            .ResourceProviders.Should()
            .BeEquivalentTo([
                new GitResourceProvider
                {
                    Name = "official",
                    Type = "trash-guides",
                    CloneUrl = new Uri("https://github.com/TRaSH-Guides/Guides.git"),
                    Reference = "master",
                    ReplaceDefault = true,
                },
                new GitResourceProvider
                {
                    Name = "official",
                    Type = "config-templates",
                    CloneUrl = new Uri("https://github.com/recyclarr/config-templates.git"),
                    Reference = "main",
                    ReplaceDefault = true,
                },
            ]);
    }

    [Test, AutoMockData]
    public void Transform_skips_repositories_with_null_clone_urls(
        RepositoriesToResourceProvidersDeprecationCheck sut
    )
    {
        var settings = new RecyclarrSettings
        {
            Repositories = new Repositories
            {
                TrashGuides = new TrashRepository { CloneUrl = null },
                ConfigTemplates = new ConfigTemplateRepository { CloneUrl = null },
            },
        };

        var result = sut.Transform(settings);

        result.ResourceProviders.Should().BeEmpty();
        result.Repositories.Should().BeNull();
    }
}
