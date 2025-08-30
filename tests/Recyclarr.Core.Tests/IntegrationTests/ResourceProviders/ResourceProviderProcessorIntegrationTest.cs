using Recyclarr.Core.TestLibrary;
using Recyclarr.ResourceProviders;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Core.Tests.IntegrationTests.ResourceProviders;

internal sealed class ResourceProviderProcessorIntegrationTest : IntegrationTestFixture
{
    [Test]
    public async Task Process_resource_providers_with_configured_settings()
    {
        // Arrange
        var settings = Substitute.For<ISettings<ResourceProviderSettings>>();
        settings.Value.Returns(new ResourceProviderSettings
        {
            TrashGuides =
            [
                new GitRepositorySource { Name = "trash-guides-1" },
                new GitRepositorySource { Name = "trash-guides-2" }
            ],
            ConfigTemplates =
            [
                new GitRepositorySource { Name = "config-templates-1" }
            ]
        });

        var processor = Resolve<ResourceProviderProcessor>();

        // Act & Assert - Should not throw
        var act = () => processor.ProcessResourceProviders(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Process_handles_empty_settings()
    {
        // Arrange
        var settings = Substitute.For<ISettings<ResourceProviderSettings>>();
        settings.Value.Returns(new ResourceProviderSettings());

        var processor = Resolve<ResourceProviderProcessor>();

        // Act & Assert - Should not throw
        var act = () => processor.ProcessResourceProviders(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Process_with_valid_trash_guides_configuration()
    {
        // Arrange
        var settings = Substitute.For<ISettings<ResourceProviderSettings>>();
        settings.Value.Returns(new ResourceProviderSettings
        {
            TrashGuides = [new GitRepositorySource { Name = "working-provider" }]
        });

        var processor = Resolve<ResourceProviderProcessor>();

        // Act & Assert - Should not throw
        var act = () => processor.ProcessResourceProviders(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Process_respects_cancellation_token()
    {
        // Arrange
        var settings = Substitute.For<ISettings<ResourceProviderSettings>>();
        settings.Value.Returns(new ResourceProviderSettings
        {
            TrashGuides = [new GitRepositorySource { Name = "test-provider" }]
        });

        var processor = Resolve<ResourceProviderProcessor>();

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var cancelledToken = cts.Token;

        // Act & Assert
        var act = () => processor.ProcessResourceProviders(cancelledToken);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task Process_handles_mixed_provider_types()
    {
        // Arrange
        var trashGuidesConfig = new GitRepositorySource
        {
            Name = "trash-guides"
        };
        var configTemplatesConfig = new GitRepositorySource
        {
            Name = "config-templates"
        };

        var settings = Substitute.For<ISettings<ResourceProviderSettings>>();
        settings.Value.Returns(new ResourceProviderSettings
        {
            TrashGuides = [trashGuidesConfig],
            ConfigTemplates = [configTemplatesConfig]
        });

        var processor = Resolve<ResourceProviderProcessor>();

        // Act & Assert - Should not throw
        var act = () => processor.ProcessResourceProviders(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Process_with_multiple_repositories_scenario()
    {
        // Arrange - Simulate realistic setup with multiple repositories
        var officialTrashGuidesConfig = new GitRepositorySource
        {
            Name = "official-trash-guides"
        };
        var customTrashGuidesConfig = new GitRepositorySource
        {
            Name = "custom-trash-guides"
        };
        var configTemplatesConfig = new GitRepositorySource
        {
            Name = "recyclarr-templates"
        };

        var settings = Substitute.For<ISettings<ResourceProviderSettings>>();
        settings.Value.Returns(new ResourceProviderSettings
        {
            TrashGuides = [officialTrashGuidesConfig, customTrashGuidesConfig],
            ConfigTemplates = [configTemplatesConfig]
        });

        var processor = Resolve<ResourceProviderProcessor>();

        // Act & Assert - Should not throw with multiple repositories
        var act = () => processor.ProcessResourceProviders(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
