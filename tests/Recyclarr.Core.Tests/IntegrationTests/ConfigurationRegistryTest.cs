using Recyclarr.Config;
using Recyclarr.Config.Filtering;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Core.TestLibrary;

namespace Recyclarr.Core.Tests.IntegrationTests;

[TestFixture]
public class ConfigurationRegistryTest : IntegrationTestFixture
{
    [Test]
    public void Use_explicit_paths_instead_of_default()
    {
        var sut = Resolve<ConfigurationRegistry>();

        Fs.AddFile(
            "manual.yml",
            new MockFileData(
                """
                radarr:
                  instance1:
                    base_url: http://localhost:7878
                    api_key: asdf
                """
            )
        );

        var result = sut.FindAndLoadConfigs(
            new ConfigFilterCriteria { ManualConfigFiles = ["manual.yml"] }
        );

        result
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(
                new
                {
                    BaseUrl = new Uri("http://localhost:7878"),
                    ApiKey = "asdf",
                    InstanceName = "instance1",
                }
            );
    }

    [Test]
    public void Throw_on_invalid_config_files()
    {
        var sut = Resolve<ConfigurationRegistry>();

        var act = () =>
            sut.FindAndLoadConfigs(new ConfigFilterCriteria { ManualConfigFiles = ["manual.yml"] });

        act.Should().ThrowExactly<InvalidConfigurationFilesException>();
    }
}
