using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Parsing;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Config.Services.Radarr;
using Recyclarr.TrashLib.Config.Services.Sonarr;
using Recyclarr.TrashLib.Processors;
using Recyclarr.TrashLib.TestLibrary;

namespace Recyclarr.TrashLib.Tests.Config.Parsing;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class ConfigRegistryTest
{
    [Test]
    public void Get_configs_by_type()
    {
        var configs = new IServiceConfiguration[]
        {
            new SonarrConfiguration {InstanceName = "one"},
            new SonarrConfiguration {InstanceName = "two"},
            new RadarrConfiguration {InstanceName = "three"}
        };

        var sut = new ConfigRegistry();
        foreach (var c in configs)
        {
            sut.Add(c);
        }

        var result = sut.GetConfigsBasedOnSettings(MockSyncSettings.Sonarr());

        result.Should().Equal(configs.Take(2));
    }

    [Test]
    public void Null_service_type_returns_configs_of_all_types()
    {
        var configs = new IServiceConfiguration[]
        {
            new SonarrConfiguration {InstanceName = "one"},
            new SonarrConfiguration {InstanceName = "two"},
            new RadarrConfiguration {InstanceName = "three"}
        };

        var sut = new ConfigRegistry();
        foreach (var c in configs)
        {
            sut.Add(c);
        }

        var result = sut.GetConfigsBasedOnSettings(MockSyncSettings.AnyService());

        result.Should().Equal(configs);
    }

    [Test]
    public void Get_empty_collection_when_no_configs_of_type()
    {
        var sut = new ConfigRegistry();
        sut.Add(new SonarrConfiguration());

        var settings = Substitute.For<ISyncSettings>();
        settings.Service.Returns(SupportedServices.Radarr);

        var result = sut.GetConfigsBasedOnSettings(settings);

        result.Should().BeEmpty();
    }

    [Test]
    public void Get_configs_by_type_and_instance_name()
    {
        var configs = new IServiceConfiguration[]
        {
            new SonarrConfiguration {InstanceName = "one"},
            new SonarrConfiguration {InstanceName = "two"},
            new RadarrConfiguration {InstanceName = "three"}
        };

        var sut = new ConfigRegistry();
        foreach (var c in configs)
        {
            sut.Add(c);
        }

        var result = sut.GetConfigsBasedOnSettings(MockSyncSettings.Sonarr("one"));

        result.Should().Equal(configs.Take(1));
    }

    [Test]
    public void Instance_matching_should_be_case_insensitive()
    {
        var configs = new IServiceConfiguration[]
        {
            new SonarrConfiguration {InstanceName = "one"}
        };

        var sut = new ConfigRegistry();
        foreach (var c in configs)
        {
            sut.Add(c);
        }

        var result = sut.GetConfigsBasedOnSettings(MockSyncSettings.AnyService("ONE"));

        result.Should().Equal(configs);
    }
}
