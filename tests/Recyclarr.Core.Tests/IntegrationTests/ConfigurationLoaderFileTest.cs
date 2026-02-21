using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Core.TestLibrary;

namespace Recyclarr.Core.Tests.IntegrationTests;

internal sealed class ConfigurationLoaderFileTest : IntegrationTestFixture
{
    [Test]
    public void Successful_file_loading()
    {
        Fs.AddFile("path/to/test_secret", "the-api-key");

        var sut = Resolve<ConfigurationLoader>();

        const string testYml = """
            sonarr:
              instance:
                api_key: !file path/to/test_secret
            """;

        var config = sut.Load(testYml);

        config
            .Should()
            .ContainSingle()
            .Which.Yaml.Should()
            .BeEquivalentTo(new ServiceConfigYaml { ApiKey = "the-api-key" });
    }

    [Test]
    public void Fail_when_file_not_found()
    {
        // We intentionally do not add the file to the mock filesystem

        var sut = Resolve<ConfigurationLoader>();

        const string testYml = """
            sonarr:
              instance:
                api_key: !file path/to/nonexistent_file
            """;

        var act = () => sut.Load(testYml);
        act.Should().Throw<ConfigParsingException>();
    }
}
