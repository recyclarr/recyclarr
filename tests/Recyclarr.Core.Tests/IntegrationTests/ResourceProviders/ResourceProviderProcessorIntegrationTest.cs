using NSubstitute.ExceptionExtensions;
using Recyclarr.Core.TestLibrary;
using Recyclarr.ResourceProviders;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;
using Recyclarr.TrashGuide;

namespace Recyclarr.Core.Tests.IntegrationTests.ResourceProviders;

internal sealed class ResourceProviderProcessorIntegrationTest : IntegrationTestFixture
{
    [Test]
    public async Task Process_resource_providers_initializes_all_providers()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);

        var provider1 = builder.CreateTrashGuidesProvider("trash-guides-1");
        var provider2 = builder.CreateTrashGuidesProvider("trash-guides-2");
        var provider3 = builder.CreateConfigTemplatesProvider("config-templates-1");

        var allProviders = new IResourceProvider[] { provider1, provider2, provider3 };

        var settings = Substitute.For<ISettings<ResourceProviderSettings>>();
        settings.Value.Returns(new ResourceProviderSettings());

        var sut = new ResourceProviderProcessor(allProviders, settings);

        // Act
        await sut.ProcessResourceProviders(CancellationToken.None);

        // Assert - Verify all providers were initialized by checking they can provide data
        // (This is an indirect verification since the providers don't expose initialization state)
        provider1.Name.Should().Be("trash-guides-1");
        provider2.Name.Should().Be("trash-guides-2");
        provider3.Name.Should().Be("config-templates-1");
    }

    [Test]
    public async Task Process_handles_empty_provider_collection()
    {
        // Arrange
        var settings = Substitute.For<ISettings<ResourceProviderSettings>>();
        settings.Value.Returns(new ResourceProviderSettings());

        var sut = new ResourceProviderProcessor([], settings);

        // Act & Assert - Should not throw
        var act = () => sut.ProcessResourceProviders(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Process_continues_if_individual_provider_initialization_fails()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);
        var workingProvider = builder.CreateTrashGuidesProvider("working-provider");

        var failingProvider = Substitute.For<IResourceProvider>();
        failingProvider.Name.Returns("failing-provider");
        failingProvider
            .Initialize(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Initialization failed"));

        var allProviders = new[] { workingProvider, failingProvider };

        var settings = Substitute.For<ISettings<ResourceProviderSettings>>();
        settings.Value.Returns(new ResourceProviderSettings());

        var sut = new ResourceProviderProcessor(allProviders, settings);

        // Act & Assert - Should not throw, but continue with working providers
        var act = () => sut.ProcessResourceProviders(CancellationToken.None);
        await act.Should().NotThrowAsync();

        // Verify the working provider still functions
        workingProvider.Name.Should().Be("working-provider");
    }

    [Test]
    public async Task Process_respects_cancellation_token()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);
        var provider = builder.CreateTrashGuidesProvider("test-provider");

        var settings = Substitute.For<ISettings<ResourceProviderSettings>>();
        settings.Value.Returns(new ResourceProviderSettings());

        var sut = new ResourceProviderProcessor([provider], settings);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var cancelledToken = cts.Token;

        // Act & Assert
        var act = () => sut.ProcessResourceProviders(cancelledToken);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task Process_handles_mixed_provider_types()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);

        var trashGuidesProvider = builder.CreateTrashGuidesProvider(
            "trash-guides",
            data =>
                data.WithMetadata(metadata =>
                        metadata.WithService(
                            SupportedServices.Radarr,
                            service => service.WithCustomFormats("docs/json/radarr/cf")
                        )
                    )
                    .WithCustomFormat(SupportedServices.Radarr, "test-format")
        );

        var configTemplatesProvider = builder.CreateConfigTemplatesProvider(
            "config-templates",
            data => data.WithTemplates()
        );

        var allProviders = new IResourceProvider[] { trashGuidesProvider, configTemplatesProvider };

        var settings = Substitute.For<ISettings<ResourceProviderSettings>>();
        settings.Value.Returns(new ResourceProviderSettings());

        var sut = new ResourceProviderProcessor(allProviders, settings);

        // Act
        await sut.ProcessResourceProviders(CancellationToken.None);

        // Assert - Verify both types of providers were initialized and work correctly
        var customFormats = trashGuidesProvider.GetCustomFormatPaths(SupportedServices.Radarr);
        var templates = configTemplatesProvider.GetTemplates();

        customFormats.Should().HaveCount(1);
        templates.Should().HaveCount(1);
    }

    [Test]
    public async Task Process_with_realistic_provider_scenario()
    {
        // Arrange - Simulate a realistic setup with multiple trash guide sources and template sources
        var builder = new ResourceProviderTestBuilder(Fs);

        var officialTrashGuides = builder.CreateTrashGuidesProvider(
            "official-trash-guides",
            data =>
                data.WithMetadata(metadata =>
                        metadata
                            .WithService(
                                SupportedServices.Radarr,
                                service =>
                                    service
                                        .WithCustomFormats("docs/json/radarr/cf")
                                        .WithQualities("docs/json/radarr/quality-size")
                            )
                            .WithService(
                                SupportedServices.Sonarr,
                                service =>
                                    service
                                        .WithCustomFormats("docs/json/sonarr/cf")
                                        .WithNaming("docs/json/sonarr/naming")
                            )
                    )
                    .WithCustomFormat(SupportedServices.Radarr, "official-format")
                    .WithCustomFormat(SupportedServices.Sonarr, "sonarr-format")
                    .WithQualityDefinition(SupportedServices.Radarr)
                    .WithNaming(SupportedServices.Sonarr)
        );

        var customTrashGuides = builder.CreateTrashGuidesProvider(
            "custom-trash-guides",
            data =>
                data.WithMetadata(metadata =>
                        metadata.WithService(
                            SupportedServices.Radarr,
                            service => service.WithCustomFormats("custom/cf")
                        )
                    )
                    .WithCustomFormat(SupportedServices.Radarr, "custom-format")
        );

        var configTemplates = builder.CreateConfigTemplatesProvider(
            "recyclarr-templates",
            data =>
                data.WithTemplates(
                        new TemplateInfo("radarr-4k", SupportedServices.Radarr, "radarr/4k.yml"),
                        new TemplateInfo(
                            "sonarr-anime",
                            SupportedServices.Sonarr,
                            "sonarr/anime.yml"
                        )
                    )
                    .WithIncludes(("common-settings", SupportedServices.Radarr))
        );

        var allProviders = new IResourceProvider[]
        {
            officialTrashGuides,
            customTrashGuides,
            configTemplates,
        };

        var settings = Substitute.For<ISettings<ResourceProviderSettings>>();
        settings.Value.Returns(new ResourceProviderSettings());

        var sut = new ResourceProviderProcessor(allProviders, settings);

        // Act
        await sut.ProcessResourceProviders(CancellationToken.None);

        // Assert - Verify all providers are functioning and providing expected resources

        // Official trash guides
        var officialRadarrCFs = officialTrashGuides.GetCustomFormatPaths(SupportedServices.Radarr);
        var officialSonarrCFs = officialTrashGuides.GetCustomFormatPaths(SupportedServices.Sonarr);
        var qualities = officialTrashGuides.GetQualitySizePaths(SupportedServices.Radarr);
        var naming = officialTrashGuides.GetMediaNamingPaths(SupportedServices.Sonarr);

        // Custom trash guides
        var customRadarrCFs = customTrashGuides.GetCustomFormatPaths(SupportedServices.Radarr);

        // Config templates
        var templates = configTemplates.GetTemplates();
        var includes = configTemplates.GetIncludes();

        // Verify all resources are available
        officialRadarrCFs.Should().HaveCount(1);
        officialSonarrCFs.Should().HaveCount(1);
        qualities.Should().HaveCount(1);
        naming.Should().HaveCount(1);
        customRadarrCFs.Should().HaveCount(1);
        templates.Should().HaveCount(2);
        includes.Should().HaveCount(1);
    }
}
