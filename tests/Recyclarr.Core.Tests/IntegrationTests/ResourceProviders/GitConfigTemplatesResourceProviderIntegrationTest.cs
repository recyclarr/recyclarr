using Recyclarr.Core.TestLibrary;
using Recyclarr.TrashGuide;

namespace Recyclarr.Core.Tests.IntegrationTests.ResourceProviders;

internal sealed class GitConfigTemplatesResourceProviderIntegrationTest : IntegrationTestFixture
{
    [Test]
    public async Task Initialize_loads_templates_and_provides_paths()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);
        var provider = builder.CreateConfigTemplatesProvider(
            "test-provider",
            data =>
                data.WithTemplates(
                    new TemplateInfo("radarr-4k", SupportedServices.Radarr, "radarr/4k.yml"),
                    new TemplateInfo("sonarr-web", SupportedServices.Sonarr, "sonarr/web.yml")
                )
        );

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert
        provider
            .GetTemplates()
            .Should()
            .HaveCount(2)
            .And.Contain(t => t.Id == "radarr-4k" && t.Service == SupportedServices.Radarr)
            .And.Contain(t => t.Id == "sonarr-web" && t.Service == SupportedServices.Sonarr);

        // Verify template files exist
        foreach (var template in provider.GetTemplates())
        {
            template.TemplateFile.Should().NotBeNull();
            template.TemplateFile.Exists.Should().BeTrue();
        }
    }

    [Test]
    public async Task Initialize_loads_includes_and_provides_paths()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);
        var provider = builder.CreateConfigTemplatesProvider(
            "test-provider",
            data =>
                data.WithIncludes(
                    ("common-settings", SupportedServices.Radarr),
                    ("api-config", SupportedServices.Sonarr),
                    ("quality-profiles", SupportedServices.Radarr)
                )
        );

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert
        provider
            .GetIncludes()
            .Should()
            .HaveCount(3)
            .And.Contain(i => i.Id == "common-settings")
            .And.Contain(i => i.Id == "api-config")
            .And.Contain(i => i.Id == "quality-profiles");

        // Verify include files exist
        foreach (var include in provider.GetIncludes())
        {
            include.IncludeFile.Should().NotBeNull();
            include.IncludeFile.Exists.Should().BeTrue();
        }
    }

    [Test]
    public async Task Handles_missing_templates_file_gracefully()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);
        var provider = builder.CreateConfigTemplatesProvider("test-provider");

        // Act & Assert - Should not throw
        var act = () => provider.Initialize(CancellationToken.None);
        await act.Should().NotThrowAsync();

        // Should return empty collections
        provider.GetTemplates().Should().BeEmpty();
        provider.GetIncludes().Should().BeEmpty();
    }

    [Test]
    public async Task Handles_missing_includes_file_gracefully()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);
        var provider = builder.CreateConfigTemplatesProvider(
            "test-provider",
            data => data.WithTemplates()
        // Intentionally not adding includes
        );

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert
        provider.GetTemplates().Should().HaveCount(1);
        provider.GetIncludes().Should().BeEmpty();
    }

    [Test]
    public async Task Template_paths_are_correctly_resolved()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);
        var provider = builder.CreateConfigTemplatesProvider(
            "test-provider",
            data => data.WithTemplates()
        );

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert
        var template = provider.GetTemplates().Should().ContainSingle().Subject;
        template.TemplateFile.Name.Should().Be("template.yml");
        template.TemplateFile.Directory!.Name.Should().Be("deep");
        template.TemplateFile.FullName.Should().Contain("radarr/nested/deep/template.yml");
    }

    [Test]
    public async Task Include_paths_are_correctly_resolved()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);
        var provider = builder.CreateConfigTemplatesProvider(
            "test-provider",
            data => data.WithIncludes(("nested-include", SupportedServices.Radarr))
        );

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert
        var include = provider.GetIncludes().Should().ContainSingle().Subject;
        include.IncludeFile.Name.Should().Be("nested-include.yml");
        include.IncludeFile.Directory!.Name.Should().Be("includes");
        include.IncludeFile.FullName.Should().Contain("includes/nested-include.yml");
    }

    [Test]
    public async Task Provider_name_matches_configured_name()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);
        var provider = builder.CreateConfigTemplatesProvider("my-template-provider");

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert
        provider.Name.Should().Be("my-template-provider");
    }

    [Test]
    public async Task Supports_templates_for_both_services()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);
        var provider = builder.CreateConfigTemplatesProvider(
            "test-provider",
            data =>
                data.WithTemplates(
                    new TemplateInfo("radarr-uhd", SupportedServices.Radarr, "radarr/uhd.yml"),
                    new TemplateInfo("radarr-hd", SupportedServices.Radarr, "radarr/hd.yml"),
                    new TemplateInfo("sonarr-anime", SupportedServices.Sonarr, "sonarr/anime.yml"),
                    new TemplateInfo("sonarr-web", SupportedServices.Sonarr, "sonarr/web.yml")
                )
        );

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert
        var templates = provider.GetTemplates();

        templates
            .Where(t => t.Service == SupportedServices.Radarr)
            .Should()
            .HaveCount(2)
            .And.Subject.Select(t => t.Id)
            .Should()
            .BeEquivalentTo("radarr-uhd", "radarr-hd");

        templates
            .Where(t => t.Service == SupportedServices.Sonarr)
            .Should()
            .HaveCount(2)
            .And.Subject.Select(t => t.Id)
            .Should()
            .BeEquivalentTo("sonarr-anime", "sonarr-web");
    }

    [Test]
    public async Task Templates_and_includes_can_coexist()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);
        var provider = builder.CreateConfigTemplatesProvider(
            "test-provider",
            data => data.WithTemplates().WithIncludes(("test-include", SupportedServices.Radarr))
        );

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert
        provider
            .GetTemplates()
            .Should()
            .HaveCount(1)
            .And.Subject.First()
            .Id.Should()
            .Be("test-template");
        provider
            .GetIncludes()
            .Should()
            .HaveCount(1)
            .And.Subject.First()
            .Id.Should()
            .Be("test-include");
    }
}
