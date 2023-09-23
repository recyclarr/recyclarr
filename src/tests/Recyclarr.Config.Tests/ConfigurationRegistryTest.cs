using Recyclarr.Config.ExceptionTypes;
using Recyclarr.Config.Models;
using Recyclarr.Config.Parsing.ErrorHandling;
using Recyclarr.Config.TestLibrary;

namespace Recyclarr.Config.Tests;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigurationRegistryTest : ConfigIntegrationFixture
{
    [Test]
    public void Use_explicit_paths_instead_of_default()
    {
        var sut = Resolve<ConfigurationRegistry>();

        Fs.AddFile("manual.yml", new MockFileData(
            """
            radarr:
              instance1:
                base_url: http://localhost:7878
                api_key: asdf
            """));

        var result = sut.FindAndLoadConfigs(new ConfigFilterCriteria
        {
            ManualConfigFiles = new[] {"manual.yml"}
        });

        result.Should().BeEquivalentTo(new[]
        {
            new RadarrConfiguration
            {
                BaseUrl = new Uri("http://localhost:7878"),
                ApiKey = "asdf",
                InstanceName = "instance1"
            }
        });
    }

    [Test]
    public void Throw_on_invalid_config_files()
    {
        var sut = Resolve<ConfigurationRegistry>();

        var act = () => sut.FindAndLoadConfigs(new ConfigFilterCriteria
        {
            ManualConfigFiles = new[] {"manual.yml"}
        });

        act.Should().ThrowExactly<InvalidConfigurationFilesException>();
    }

    [Test]
    public void Throw_on_invalid_instances()
    {
        var sut = Resolve<ConfigurationRegistry>();

        Fs.AddFile("manual.yml", new MockFileData(
            """
            radarr:
              instance1:
                base_url: http://localhost:7878
                api_key: asdf
            """));

        var act = () => sut.FindAndLoadConfigs(new ConfigFilterCriteria
        {
            ManualConfigFiles = new[] {"manual.yml"},
            Instances = new[] {"instance1", "instance2"}
        });

        act.Should().ThrowExactly<InvalidInstancesException>()
            .Which.InstanceNames.Should().BeEquivalentTo("instance2");
    }

    [Test]
    public void Throw_on_split_instances()
    {
        var sut = Resolve<ConfigurationRegistry>();

        Fs.AddFile("manual.yml", new MockFileData(
            """
            radarr:
              instance1:
                base_url: http://localhost:7878
                api_key: asdf
              instance2:
                base_url: http://localhost:7878
                api_key: asdf
            """));

        var act = () => sut.FindAndLoadConfigs(new ConfigFilterCriteria
        {
            ManualConfigFiles = new[] {"manual.yml"}
        });

        act.Should().ThrowExactly<SplitInstancesException>()
            .Which.InstanceNames.Should().BeEquivalentTo("instance1", "instance2");
    }

    [Test]
    public void Duplicate_instance_names_are_prohibited()
    {
        var sut = Resolve<ConfigurationRegistry>();

        Fs.AddFile("config1.yml", new MockFileData(
            """
            radarr:
              unique_name1:
                base_url: http://localhost:7879
                api_key: fdsa
              same_instance_name:
                base_url: http://localhost:7878
                api_key: asdf
            """));

        Fs.AddFile("config2.yml", new MockFileData(
            """
            radarr:
              same_instance_name:
                base_url: http://localhost:7879
                api_key: fdsa
              unique_name2:
                base_url: http://localhost:7879
                api_key: fdsa
            """));

        var act = () => sut.FindAndLoadConfigs(new ConfigFilterCriteria
        {
            ManualConfigFiles = new[] {"config1.yml", "config2.yml"}
        });

        act.Should().ThrowExactly<DuplicateInstancesException>()
            .Which.InstanceNames.Should().BeEquivalentTo("same_instance_name");
    }
}
