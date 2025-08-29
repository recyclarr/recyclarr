using Recyclarr.Core.TestLibrary;
using Recyclarr.TrashGuide;

namespace Recyclarr.Core.Tests.IntegrationTests.ResourceProviders;

internal sealed class GitTrashGuidesResourceProviderIntegrationTest : IntegrationTestFixture
{
    [Test]
    public async Task Initialize_loads_metadata_and_provides_resource_paths()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);
        var provider = builder.CreateTrashGuidesProvider(
            "test-provider",
            data =>
                data.WithMetadata(metadata =>
                        metadata.WithService(
                            SupportedServices.Radarr,
                            service =>
                                service
                                    .WithCustomFormats("docs/json/radarr/cf")
                                    .WithQualities("docs/json/radarr/quality-size")
                                    .WithNaming("docs/json/radarr/naming")
                        )
                    )
                    .WithCustomFormat(SupportedServices.Radarr, "test-format")
                    .WithQualityDefinition(SupportedServices.Radarr)
                    .WithNaming(SupportedServices.Radarr)
        );

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert
        provider
            .GetCustomFormatPaths(SupportedServices.Radarr)
            .Should()
            .HaveCount(1)
            .And.Subject.First()
            .Name.Should()
            .Be("test-format.json");

        provider
            .GetQualitySizePaths(SupportedServices.Radarr)
            .Should()
            .HaveCount(1)
            .And.Subject.First()
            .Name.Should()
            .Be("quality-size.json");

        provider
            .GetMediaNamingPaths(SupportedServices.Radarr)
            .Should()
            .HaveCount(1)
            .And.Subject.First()
            .Name.Should()
            .Be("naming.json");
    }

    [Test]
    public async Task Provides_custom_format_categories_from_metadata()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);
        var provider = builder.CreateTrashGuidesProvider(
            "test-provider",
            data =>
                data.WithMetadata(metadata =>
                        metadata.WithService(
                            SupportedServices.Radarr,
                            service => service.WithCustomFormats("docs/json/radarr/cf")
                        )
                    )
                    .WithCustomFormat(SupportedServices.Radarr, "test-format")
        );

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert
        var categories = provider.GetCategoryData();
        categories.Should().NotBeEmpty();
    }

    [Test]
    public async Task Supports_multiple_services()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);
        var provider = builder.CreateTrashGuidesProvider(
            "test-provider",
            data =>
                data.WithMetadata(metadata =>
                        metadata
                            .WithService(
                                SupportedServices.Radarr,
                                service => service.WithCustomFormats("docs/json/radarr/cf")
                            )
                            .WithService(
                                SupportedServices.Sonarr,
                                service =>
                                    service
                                        .WithCustomFormats("docs/json/sonarr/cf")
                                        .WithQualities("docs/json/sonarr/quality-size")
                            )
                    )
                    .WithCustomFormat(SupportedServices.Radarr, "radarr-format")
                    .WithCustomFormat(SupportedServices.Sonarr, "sonarr-format")
                    .WithQualityDefinition(SupportedServices.Sonarr)
        );

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert
        provider
            .GetCustomFormatPaths(SupportedServices.Radarr)
            .Should()
            .HaveCount(1)
            .And.Subject.First()
            .Name.Should()
            .Be("radarr-format.json");

        provider
            .GetCustomFormatPaths(SupportedServices.Sonarr)
            .Should()
            .HaveCount(1)
            .And.Subject.First()
            .Name.Should()
            .Be("sonarr-format.json");

        provider
            .GetQualitySizePaths(SupportedServices.Sonarr)
            .Should()
            .HaveCount(1)
            .And.Subject.First()
            .Name.Should()
            .Be("quality-size.json");
    }

    [Test]
    public async Task Returns_empty_collections_for_unsupported_service()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);
        var provider = builder.CreateTrashGuidesProvider(
            "test-provider",
            data =>
                data.WithMetadata(metadata =>
                        metadata.WithService(
                            SupportedServices.Radarr,
                            service => service.WithCustomFormats("docs/json/radarr/cf")
                        )
                    )
                    .WithCustomFormat(SupportedServices.Radarr, "test-format")
        );

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert - Sonarr is not configured in metadata
        provider.GetCustomFormatPaths(SupportedServices.Sonarr).Should().BeEmpty();
        provider.GetQualitySizePaths(SupportedServices.Sonarr).Should().BeEmpty();
        provider.GetMediaNamingPaths(SupportedServices.Sonarr).Should().BeEmpty();
    }

    [Test]
    public async Task Handles_missing_metadata_file_gracefully()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);
        var provider = builder.CreateTrashGuidesProvider("test-provider");

        // Act & Assert - Should not throw
        var act = () => provider.Initialize(CancellationToken.None);
        await act.Should().NotThrowAsync();

        // All paths should be empty without metadata
        provider.GetCustomFormatPaths(SupportedServices.Radarr).Should().BeEmpty();
        provider.GetQualitySizePaths(SupportedServices.Radarr).Should().BeEmpty();
        provider.GetMediaNamingPaths(SupportedServices.Radarr).Should().BeEmpty();
    }

    [Test]
    public async Task Provider_name_matches_configured_name()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);
        var provider = builder.CreateTrashGuidesProvider("my-custom-provider");

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert
        provider.Name.Should().Be("my-custom-provider");
    }

    [Test]
    public async Task Multiple_custom_formats_in_same_directory()
    {
        // Arrange
        var builder = new ResourceProviderTestBuilder(Fs);
        var provider = builder.CreateTrashGuidesProvider(
            "test-provider",
            data =>
                data.WithMetadata(metadata =>
                        metadata.WithService(
                            SupportedServices.Radarr,
                            service => service.WithCustomFormats("docs/json/radarr/cf")
                        )
                    )
                    .WithCustomFormat(SupportedServices.Radarr, "format1")
                    .WithCustomFormat(SupportedServices.Radarr, "format2")
                    .WithCustomFormat(SupportedServices.Radarr, "format3")
        );

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert
        provider
            .GetCustomFormatPaths(SupportedServices.Radarr)
            .Should()
            .HaveCount(3)
            .And.Subject.Select(cf => cf.Name)
            .Should()
            .BeEquivalentTo("format1.json", "format2.json", "format3.json");
    }
}
