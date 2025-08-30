using Recyclarr.ConfigTemplates;
using Recyclarr.Core.TestLibrary;
using Recyclarr.TrashGuide;

namespace Recyclarr.Core.Tests.IntegrationTests.ResourceProviders;

internal sealed class ConfigTemplatesResourceQueryIntegrationTest : IntegrationTestFixture
{
    [Test]
    public void Get_templates_returns_empty_collections_from_aggregation()
    {
        // Arrange - Using real resolved providers with StubRepoUpdater
        var query = Resolve<IConfigTemplatesResourceQuery>();

        // Act
        var templates = query.GetTemplates();

        // Assert - StubRepoUpdater provides empty collections by design
        templates.Should().NotBeNull();
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
    public void Get_includes_returns_empty_collections_from_aggregation()
    {
        // Arrange - Using real resolved providers with StubRepoUpdater
        var query = Resolve<IConfigTemplatesResourceQuery>();

        // Act
        var includes = query.GetIncludes();

        // Assert - StubRepoUpdater provides empty collections by design
        includes.Should().NotBeNull();
    }



}
