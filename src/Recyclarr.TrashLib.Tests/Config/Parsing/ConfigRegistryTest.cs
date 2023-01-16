using FluentAssertions;
using NUnit.Framework;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Parsing;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Services.Radarr.Config;
using Recyclarr.TrashLib.Services.Sonarr.Config;

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

        var result = sut.GetConfigsOfType(SupportedServices.Sonarr);

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

        var result = sut.GetConfigsOfType(null);

        result.Should().Equal(configs);
    }

    [Test]
    public void Get_empty_collection_when_no_configs_of_type()
    {
        var sut = new ConfigRegistry();
        sut.Add(new SonarrConfiguration());

        var result = sut.GetConfigsOfType(SupportedServices.Radarr);

        result.Should().BeEmpty();
    }
}
