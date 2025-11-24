using Recyclarr.Settings.Deprecations;
using Recyclarr.Settings.Models;
using Recyclarr.TestLibrary;

namespace Recyclarr.Core.Tests.Settings.Deprecations;

internal sealed class RepositoriesToResourceProvidersDeprecationCheckTest
{
    [Test, AutoMockData]
    public void Transform_logs_deprecation_warning_and_converts_repositories(
        RepositoriesToResourceProvidersDeprecationCheck sut
    )
    {
        var logger = new TestableLogger();
        var sutWithLogger = new RepositoriesToResourceProvidersDeprecationCheck(logger);

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

        var result = sutWithLogger.Transform(settings);

        // Should log deprecation warning
        logger
            .Messages.Should()
            .ContainSingle()
            .Which.Should()
            .Contain("DEPRECATED: The `repositories` setting");

        // Should transform and clear old format
        result.Repositories.Should().BeNull();

        // Should create new resource providers
        result
            .ResourceProviders.TrashGuides.Should()
            .BeEquivalentTo([
                new GitRepositorySource
                {
                    Name = "official",
                    CloneUrl = new Uri("https://github.com/TRaSH-Guides/Guides.git"),
                    Reference = "master",
                },
            ]);

        result
            .ResourceProviders.ConfigTemplates.Should()
            .BeEquivalentTo([
                new GitRepositorySource
                {
                    Name = "official",
                    CloneUrl = new Uri("https://github.com/recyclarr/config-templates.git"),
                    Reference = "main",
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

        result.ResourceProviders.TrashGuides.Should().BeEmpty();
        result.ResourceProviders.ConfigTemplates.Should().BeEmpty();
        result.Repositories.Should().BeNull();
    }
}
