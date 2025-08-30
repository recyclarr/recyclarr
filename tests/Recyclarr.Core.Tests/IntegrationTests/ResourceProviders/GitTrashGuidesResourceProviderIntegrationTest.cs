using Recyclarr.Core.TestLibrary;
using Recyclarr.TrashGuide;
using Recyclarr.Settings.Models;

namespace Recyclarr.Core.Tests.IntegrationTests.ResourceProviders;

internal sealed class GitTrashGuidesResourceProviderIntegrationTest : IntegrationTestFixture
{
    [Test]
    public async Task Initialize_loads_metadata_and_provides_resource_paths()
    {
        // Arrange
        var config = new GitRepositorySource { Name = "test-provider" };
        var provider = Resolve<TrashGuidesGitRepository>();

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert
        provider
            .GetCustomFormatPaths(SupportedServices.Radarr)
            .Should()
            .HaveCount(1)
            .And.Subject.First()
            .Name.Should()
            .Be("cf");

        provider
            .GetQualitySizePaths(SupportedServices.Radarr)
            .Should()
            .HaveCount(1)
            .And.Subject.First()
            .Name.Should()
            .Be("quality-size");

        provider
            .GetMediaNamingPaths(SupportedServices.Radarr)
            .Should()
            .HaveCount(1)
            .And.Subject.First()
            .Name.Should()
            .Be("naming");
    }

    [Test]
    public async Task Provides_custom_format_categories_from_metadata()
    {
        // Arrange
        var config = new GitRepositorySource { Name = "test-provider" };
        var provider = Resolve<TrashGuidesGitRepository>();

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
        var config = new GitRepositorySource { Name = "test-provider" };
        var provider = Resolve<TrashGuidesGitRepository>();

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert
        provider
            .GetCustomFormatPaths(SupportedServices.Radarr)
            .Should()
            .HaveCount(1)
            .And.Subject.First()
            .Name.Should()
            .Be("cf");

        provider
            .GetCustomFormatPaths(SupportedServices.Sonarr)
            .Should()
            .HaveCount(1)
            .And.Subject.First()
            .Name.Should()
            .Be("cf");

        provider
            .GetQualitySizePaths(SupportedServices.Sonarr)
            .Should()
            .HaveCount(1)
            .And.Subject.First()
            .Name.Should()
            .Be("quality-size");
    }

    [Test]
    public async Task Returns_paths_for_all_configured_services()
    {
        // Arrange
        var config = new GitRepositorySource { Name = "test-provider" };
        var provider = Resolve<TrashGuidesGitRepository>();

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert - Both Radarr and Sonarr are configured in metadata
        provider.GetCustomFormatPaths(SupportedServices.Radarr).Should().NotBeEmpty();
        provider.GetCustomFormatPaths(SupportedServices.Sonarr).Should().NotBeEmpty();
        provider.GetQualitySizePaths(SupportedServices.Radarr).Should().NotBeEmpty();
        provider.GetQualitySizePaths(SupportedServices.Sonarr).Should().NotBeEmpty();
    }

    [Test]
    public async Task Initialize_succeeds_with_valid_repository()
    {
        // Arrange - StubRepoUpdater provides valid metadata.json
        var config = new GitRepositorySource { Name = "test-provider" };
        var provider = Resolve<TrashGuidesGitRepository>();

        // Act & Assert - Should not throw
        var act = () => provider.Initialize(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Provider_name_matches_configured_name()
    {
        // Arrange
        var config = new GitRepositorySource { Name = "my-custom-provider" };
        var provider = Resolve<TrashGuidesGitRepository>();

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert
        provider.Name.Should().Be("my-custom-provider");
    }

    [Test]
    public async Task Provides_paths_from_metadata_configuration()
    {
        // Arrange
        var config = new GitRepositorySource { Name = "test-provider" };
        var provider = Resolve<TrashGuidesGitRepository>();

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert - Verify paths match metadata.json structure
        provider.GetCustomFormatPaths(SupportedServices.Radarr).Should().NotBeEmpty();
        provider.GetQualitySizePaths(SupportedServices.Radarr).Should().NotBeEmpty();
        provider.GetMediaNamingPaths(SupportedServices.Radarr).Should().NotBeEmpty();
    }
}
