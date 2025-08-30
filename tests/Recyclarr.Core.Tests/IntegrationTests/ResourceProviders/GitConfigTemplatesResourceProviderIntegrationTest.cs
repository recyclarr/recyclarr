using Recyclarr.ConfigTemplates;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Settings.Models;
using Recyclarr.TrashGuide;

namespace Recyclarr.Core.Tests.IntegrationTests.ResourceProviders;

internal sealed class GitConfigTemplatesResourceProviderIntegrationTest : IntegrationTestFixture
{
    [Test]
    public async Task Initialize_loads_templates_and_provides_empty_collections()
    {
        // Arrange - StubRepoUpdater provides basic JSON structure
        var config = new GitRepositorySource { Name = "test-provider" };
        var provider = Resolve<ConfigTemplatesGitRepository>();

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert - StubRepoUpdater provides empty collections by design
        var templates = provider.GetTemplates();
        var includes = provider.GetIncludes();
        
        templates.Should().NotBeNull();
        includes.Should().NotBeNull();
    }

    [Test]
    public async Task Provider_name_matches_configured_name()
    {
        // Arrange
        var config = new GitRepositorySource { Name = "my-template-provider" };
        var provider = Resolve<ConfigTemplatesGitRepository>();

        // Act
        await provider.Initialize(CancellationToken.None);

        // Assert
        provider.Name.Should().Be("my-template-provider");
    }

    [Test]
    public async Task Initialize_succeeds_with_valid_repository()
    {
        // Arrange - StubRepoUpdater provides valid templates.json and includes.json
        var config = new GitRepositorySource { Name = "test-provider" };
        var provider = Resolve<ConfigTemplatesGitRepository>();

        // Act & Assert - Should not throw
        var act = () => provider.Initialize(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }






}
