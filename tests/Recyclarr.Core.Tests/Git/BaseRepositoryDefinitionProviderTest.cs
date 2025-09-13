using Recyclarr.ResourceProviders.Git;
using Recyclarr.Settings;
using Recyclarr.Settings.Models;

namespace Recyclarr.Core.Tests.Git;

internal sealed class BaseRepositoryDefinitionProviderTest
{
    internal sealed class TestableRepositoryProvider : BaseRepositoryDefinitionProvider
    {
        public override string RepositoryType => "test-type";

        protected override IReadOnlyCollection<IUnderlyingResourceProvider> GetUserRepositories()
        {
            return TestUserRepositories;
        }

        protected override GitRepositorySource CreateOfficialRepository()
        {
            return new GitRepositorySource
            {
                Name = "official",
                CloneUrl = new Uri("https://github.com/test/official.git"),
                Reference = "main",
            };
        }

        // Allow tests to control what user repositories are returned
        public IReadOnlyCollection<IUnderlyingResourceProvider> TestUserRepositories { get; set; } =
        [];
    }

    [Test, AutoMockData]
    public void Adds_implicit_official_repo_when_user_has_no_repositories(
        [Frozen] ISettings<ResourceProviderSettings> settings,
        TestableRepositoryProvider sut
    )
    {
        // Arrange: No user repositories
        sut.TestUserRepositories = [];

        // Act
        var result = sut.GetRepositoryDefinitions().ToList();

        // Assert: Should contain only the implicit official repo
        result.Should().HaveCount(1);
        result[0]
            .Should()
            .BeEquivalentTo(
                new GitRepositorySource
                {
                    Name = "official",
                    CloneUrl = new Uri("https://github.com/test/official.git"),
                    Reference = "main",
                }
            );
    }

    [Test, AutoMockData]
    public void Adds_implicit_official_repo_first_when_user_has_custom_repositories(
        [Frozen] ISettings<ResourceProviderSettings> settings,
        TestableRepositoryProvider sut
    )
    {
        // Arrange: User has custom repositories but no explicit "official"
        var userRepo = new GitRepositorySource
        {
            Name = "my-custom",
            CloneUrl = new Uri("https://github.com/user/custom.git"),
            Reference = "main",
        };

        sut.TestUserRepositories = [userRepo];

        // Act
        var result = sut.GetRepositoryDefinitions().ToList();

        // Assert: Should have implicit official first, then user repos
        result.Should().HaveCount(2);
        result[0]
            .Should()
            .BeEquivalentTo(
                new GitRepositorySource
                {
                    Name = "official",
                    CloneUrl = new Uri("https://github.com/test/official.git"),
                    Reference = "main",
                }
            );
        result[1].Should().BeEquivalentTo(userRepo);
    }

    [Test, AutoMockData]
    public void Does_NOT_add_implicit_official_when_user_explicitly_configures_official(
        [Frozen] ISettings<ResourceProviderSettings> settings,
        TestableRepositoryProvider sut
    )
    {
        // Arrange: User explicitly configures an "official" repo (should override implicit)
        var explicitOfficialRepo = new GitRepositorySource
        {
            Name = "official",
            CloneUrl = new Uri("https://github.com/user/custom-official.git"),
            Reference = "custom-branch",
        };

        var anotherRepo = new GitRepositorySource
        {
            Name = "another",
            CloneUrl = new Uri("https://github.com/user/another.git"),
            Reference = "main",
        };

        sut.TestUserRepositories = [explicitOfficialRepo, anotherRepo];

        // Act
        var result = sut.GetRepositoryDefinitions().ToList();

        // Assert: Should contain ONLY user repositories, no implicit official
        result.Should().HaveCount(2);
        result[0].Should().BeEquivalentTo(explicitOfficialRepo);
        result[1].Should().BeEquivalentTo(anotherRepo);
    }

    [Test, AutoMockData]
    public void User_explicit_official_repo_preserves_user_specified_order(
        [Frozen] ISettings<ResourceProviderSettings> settings,
        TestableRepositoryProvider sut
    )
    {
        // Arrange: User puts "official" repo in the middle/end (not first)
        var firstRepo = new GitRepositorySource
        {
            Name = "my-priority",
            CloneUrl = new Uri("https://github.com/user/priority.git"),
            Reference = "main",
        };

        var explicitOfficialRepo = new GitRepositorySource
        {
            Name = "official",
            CloneUrl = new Uri("https://github.com/user/custom-official.git"),
            Reference = "custom",
        };

        sut.TestUserRepositories = [firstRepo, explicitOfficialRepo];

        // Act
        var result = sut.GetRepositoryDefinitions().ToList();

        // Assert: Should preserve user ordering (first repo wins precedence)
        result.Should().HaveCount(2);
        result[0].Should().BeEquivalentTo(firstRepo);
        result[1].Should().BeEquivalentTo(explicitOfficialRepo);
    }
}
