using Autofac;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Recyclarr.Cli.Pipelines.ReleaseProfile.Api;
using Recyclarr.Cli.Pipelines.ReleaseProfile.Api.Objects;
using Recyclarr.Cli.TestLibrary;
using Recyclarr.TestLibrary.Autofac;
using Recyclarr.TrashLib.Compatibility.Sonarr;
using Recyclarr.TrashLib.Config;

namespace Recyclarr.Cli.Tests.Pipelines.ReleaseProfile.Api;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SonarrReleaseProfileCompatibilityHandlerTest : CliIntegrationFixture
{
    private static JObject SerializeJson<T>(T obj)
    {
        JsonSerializerSettings jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        return JObject.Parse(JsonConvert.SerializeObject(obj, jsonSettings));
    }

    protected override void RegisterTypes(ContainerBuilder builder)
    {
        base.RegisterTypes(builder);
        builder.RegisterMockFor<ISonarrCapabilityFetcher>();
    }

    [Test]
    public void Receive_v1_to_v2()
    {
        var sut = Resolve<SonarrReleaseProfileCompatibilityHandler>();
        var dataV1 = new SonarrReleaseProfileV1 {Ignored = "one,two,three"};

        var result = sut.CompatibleReleaseProfileForReceiving(SerializeJson(dataV1));

        result.Should().BeEquivalentTo(new SonarrReleaseProfile
        {
            Ignored = new List<string> {"one", "two", "three"}
        });
    }

    [Test]
    public void Receive_v2_to_v2()
    {
        var sut = Resolve<SonarrReleaseProfileCompatibilityHandler>();
        var dataV2 = new SonarrReleaseProfile {Ignored = new List<string> {"one", "two", "three"}};

        var result = sut.CompatibleReleaseProfileForReceiving(SerializeJson(dataV2));

        result.Should().BeEquivalentTo(dataV2);
    }

    [Test]
    public async Task Send_v2_to_v1()
    {
        var capabilityChecker = Resolve<ISonarrCapabilityFetcher>();
        capabilityChecker.GetCapabilities(default!).ReturnsForAnyArgs(new SonarrCapabilities
        {
            ArraysNeededForReleaseProfileRequiredAndIgnored = false
        });

        var sut = Resolve<SonarrReleaseProfileCompatibilityHandler>();
        var data = new SonarrReleaseProfile {Ignored = new List<string> {"one", "two", "three"}};

        var result = await sut.CompatibleReleaseProfileForSending(Substitute.For<IServiceConfiguration>(), data);

        result.Should().BeEquivalentTo(new SonarrReleaseProfileV1 {Ignored = "one,two,three"});
    }

    [Test]
    public async Task Send_v2_to_v2()
    {
        var capabilityChecker = Resolve<ISonarrCapabilityFetcher>();
        capabilityChecker.GetCapabilities(default!).ReturnsForAnyArgs(new SonarrCapabilities
        {
            ArraysNeededForReleaseProfileRequiredAndIgnored = true
        });

        var sut = Resolve<SonarrReleaseProfileCompatibilityHandler>();
        var data = new SonarrReleaseProfile {Ignored = new List<string> {"one", "two", "three"}};

        var result = await sut.CompatibleReleaseProfileForSending(Substitute.For<IServiceConfiguration>(), data);

        result.Should().BeEquivalentTo(data);
    }
}
