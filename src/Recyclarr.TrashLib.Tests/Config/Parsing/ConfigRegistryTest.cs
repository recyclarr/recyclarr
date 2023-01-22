using FluentAssertions;
using NUnit.Framework;
using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Parsing;
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
        var configs = new[]
        {
            new SonarrConfiguration {InstanceName = "one"},
            new SonarrConfiguration {InstanceName = "two"}
        };

        var sut = new ConfigRegistry();
        sut.Add(SupportedServices.Sonarr, configs[0]);
        sut.Add(SupportedServices.Sonarr, configs[1]);

        var result = sut.GetConfigsOfType<SonarrConfiguration>(SupportedServices.Sonarr);

        result.Should().Equal(configs);
    }

    [Test]
    public void Get_empty_collection_when_no_configs_of_type()
    {
        var sut = new ConfigRegistry();
        sut.Add(SupportedServices.Sonarr, new SonarrConfiguration());

        var result = sut.GetConfigsOfType<RadarrConfiguration>(SupportedServices.Radarr);

        result.Should().BeEmpty();
    }
}
