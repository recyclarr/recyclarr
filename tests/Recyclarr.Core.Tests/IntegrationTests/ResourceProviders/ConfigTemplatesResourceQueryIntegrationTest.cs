using Recyclarr.ConfigTemplates;
using Recyclarr.Core.TestLibrary;
using Recyclarr.TrashGuide;

namespace Recyclarr.Core.Tests.IntegrationTests.ResourceProviders;

internal sealed class ConfigTemplatesResourceQueryIntegrationTest : IntegrationTestFixture
{
    [Test]
    public async Task Get_templates_aggregates_from_multiple_providers()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);

        var provider1 = builder.CreateConfigTemplatesProvider(
            "provider1",
            data =>
                data.WithTemplates(
                    new TemplateInfo("template1", SupportedServices.Radarr, "radarr/template1.yml"),
                    new TemplateInfo("template2", SupportedServices.Sonarr, "sonarr/template2.yml")
                )
        );

        var provider2 = builder.CreateConfigTemplatesProvider(
            "provider2",
            data => data.WithTemplates()
        );

        await provider1.Initialize(CancellationToken.None);
        await provider2.Initialize(CancellationToken.None);

        var sut = new ConfigTemplatesResourceQuery([provider1, provider2], [provider1, provider2]);

        // Act
        var templates = sut.GetTemplates();

        // Assert
        templates.Should().HaveCount(3);
        templates
            .Should()
            .Contain(t => t.Id == "template1" && t.Service == SupportedServices.Radarr);
        templates
            .Should()
            .Contain(t => t.Id == "template2" && t.Service == SupportedServices.Sonarr);
        templates
            .Should()
            .Contain(t => t.Id == "template3" && t.Service == SupportedServices.Radarr);
    }

    [Test]
    public void Get_templates_returns_empty_when_no_providers()
    {
        // Arrange
        var sut = new ConfigTemplatesResourceQuery([], []);

        // Act
        var templates = sut.GetTemplates();

        // Assert
        templates.Should().BeEmpty();
    }

    [Test]
    public async Task Get_includes_aggregates_from_multiple_providers()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);

        var provider1 = builder.CreateConfigTemplatesProvider(
            "provider1",
            data =>
                data.WithIncludes(
                    ("include1", SupportedServices.Radarr),
                    ("include2", SupportedServices.Sonarr)
                )
        );

        var provider2 = builder.CreateConfigTemplatesProvider(
            "provider2",
            data => data.WithIncludes(("include3", SupportedServices.Radarr))
        );

        await provider1.Initialize(CancellationToken.None);
        await provider2.Initialize(CancellationToken.None);

        var sut = new ConfigTemplatesResourceQuery([provider1, provider2], [provider1, provider2]);

        // Act
        var includes = sut.GetIncludes();

        // Assert
        includes.Should().HaveCount(3);
        includes.Should().Contain(i => i.Id == "include1");
        includes.Should().Contain(i => i.Id == "include2");
        includes.Should().Contain(i => i.Id == "include3");
    }

    [Test]
    public async Task Get_template_paths_filters_by_service()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);

        var provider = builder.CreateConfigTemplatesProvider(
            "test-provider",
            data =>
                data.WithTemplates(
                    new TemplateInfo(
                        "radarr-template",
                        SupportedServices.Radarr,
                        "radarr/template.yml"
                    ),
                    new TemplateInfo(
                        "sonarr-template",
                        SupportedServices.Sonarr,
                        "sonarr/template.yml"
                    )
                )
        );

        await provider.Initialize(CancellationToken.None);

        var sut = new ConfigTemplatesResourceQuery([provider], [provider]);

        // Act & Assert
        var allTemplates = sut.GetTemplates().ToList();

        allTemplates
            .Where(t => t.Service == SupportedServices.Radarr)
            .Should()
            .HaveCount(1)
            .And.Subject.First()
            .Id.Should()
            .Be("radarr-template");

        allTemplates
            .Where(t => t.Service == SupportedServices.Sonarr)
            .Should()
            .HaveCount(1)
            .And.Subject.First()
            .Id.Should()
            .Be("sonarr-template");
    }

    [Test]
    public async Task Template_paths_point_to_real_files()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);

        var provider = builder.CreateConfigTemplatesProvider(
            "test-provider",
            data => data.WithTemplates()
        );

        await provider.Initialize(CancellationToken.None);

        var sut = new ConfigTemplatesResourceQuery([provider], [provider]);

        // Act & Assert
        var template = sut.GetTemplates().Should().ContainSingle().Subject;
        template.TemplateFile.Should().NotBeNull();
        template.TemplateFile!.Exists.Should().BeTrue();
        template.TemplateFile.Name.Should().Be("test.yml");
    }

    [Test]
    public async Task Handles_provider_with_no_templates_gracefully()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);

        // Provider with no templates configured
        var provider = builder.CreateConfigTemplatesProvider("empty-provider");

        await provider.Initialize(CancellationToken.None);

        var sut = new ConfigTemplatesResourceQuery([provider], [provider]);

        // Act & Assert
        sut.GetTemplates().Should().BeEmpty();
        sut.GetIncludes().Should().BeEmpty();
    }
}
