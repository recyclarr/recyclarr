using System.IO.Abstractions;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Core.TestLibrary;

namespace Recyclarr.Core.Tests.IntegrationTests;

internal sealed class ConfigurationLoaderFileTest : IntegrationTestFixture
{
    [Test]
    public void Relative_path_resolves_against_config_directory()
    {
        var secretFile = Paths.ConfigDirectory.File("secrets/api_key");
        Fs.AddFile(secretFile.FullName, "the-api-key");

        var sut = Resolve<ConfigurationLoader>();

        const string testYml = """
            sonarr:
              instance:
                api_key: !file secrets/api_key
            """;

        var config = sut.Load(testYml);

        config
            .Should()
            .ContainSingle()
            .Which.Yaml.Should()
            .BeEquivalentTo(new ServiceConfigYaml { ApiKey = "the-api-key" });
    }

    [Test]
    public void Absolute_path_is_used_as_is()
    {
        Fs.AddFile("/absolute/secrets/api_key", "the-api-key");

        var sut = Resolve<ConfigurationLoader>();

        const string testYml = """
            sonarr:
              instance:
                api_key: !file /absolute/secrets/api_key
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
