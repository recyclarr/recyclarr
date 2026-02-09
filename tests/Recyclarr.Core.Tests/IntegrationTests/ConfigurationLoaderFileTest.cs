using Recyclarr.Config.Parsing;
using Recyclarr.Core.TestLibrary;

namespace Recyclarr.Core.Tests.IntegrationTests;

[CoreDataSource]
internal sealed class ConfigurationLoaderFileTest(ConfigurationLoader sut, MockFileSystem fs)
{
    [Test]
    public void Successful_file_loading()
    {
        fs.AddFile("path/to/test_secret", "the-api-key");

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
        const string testYml = """
            sonarr:
              instance:
                api_key: !file path/to/nonexistent_file
            """;

        var result = sut.Load(testYml);

        result.Should().BeEmpty();
    }
}
