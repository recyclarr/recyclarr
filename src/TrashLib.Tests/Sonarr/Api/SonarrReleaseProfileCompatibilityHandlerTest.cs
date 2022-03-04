using System.Reactive.Linq;
using AutoMapper;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using NUnit.Framework;
using TrashLib.Sonarr;
using TrashLib.Sonarr.Api;
using TrashLib.Sonarr.Api.Objects;
using TrashLib.Startup;

namespace TrashLib.Tests.Sonarr.Api;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SonarrReleaseProfileCompatibilityHandlerTest
{
    private class TestContext : IDisposable
    {
        private readonly JsonSerializerSettings _jsonSettings;

        public TestContext()
        {
            _jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            Mapper = AutoMapperConfig.Setup();
        }

        public IMapper Mapper { get; }

        public void Dispose()
        {
        }

        public string SerializeJson<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, _jsonSettings);
        }
    }

    [Test]
    public void Receive_v1_to_v2()
    {
        using var ctx = new TestContext();

        var compat = Substitute.For<ISonarrCompatibility>();
        var dataV1 = new SonarrReleaseProfileV1 {Ignored = "one,two,three"};
        var sut = new SonarrReleaseProfileCompatibilityHandler(compat, ctx.Mapper);

        var result = sut.CompatibleReleaseProfileForReceiving(JObject.Parse(ctx.SerializeJson(dataV1)));

        result.Should().BeEquivalentTo(new SonarrReleaseProfile
        {
            Ignored = new List<string> {"one", "two", "three"}
        });
    }

    [Test]
    public void Receive_v2_to_v2()
    {
        using var ctx = new TestContext();

        var compat = Substitute.For<ISonarrCompatibility>();
        var dataV2 = new SonarrReleaseProfile {Ignored = new List<string> {"one", "two", "three"}};
        var sut = new SonarrReleaseProfileCompatibilityHandler(compat, ctx.Mapper);

        var result = sut.CompatibleReleaseProfileForReceiving(JObject.Parse(ctx.SerializeJson(dataV2)));

        result.Should().BeEquivalentTo(dataV2);
    }

    [Test]
    public async Task Send_v2_to_v1()
    {
        using var ctx = new TestContext();

        var compat = Substitute.For<ISonarrCompatibility>();
        compat.Capabilities.Returns(new[]
        {
            new SonarrCapabilities {ArraysNeededForReleaseProfileRequiredAndIgnored = false}
        }.ToObservable());

        var data = new SonarrReleaseProfile {Ignored = new List<string> {"one", "two", "three"}};
        var sut = new SonarrReleaseProfileCompatibilityHandler(compat, ctx.Mapper);

        var result = await sut.CompatibleReleaseProfileForSendingAsync(data);

        result.Should().BeEquivalentTo(new SonarrReleaseProfileV1 {Ignored = "one,two,three"});
    }

    [Test]
    public async Task Send_v2_to_v2()
    {
        using var ctx = new TestContext();

        var compat = Substitute.For<ISonarrCompatibility>();
        compat.Capabilities.Returns(new[]
        {
            new SonarrCapabilities {ArraysNeededForReleaseProfileRequiredAndIgnored = true}
        }.ToObservable());

        var data = new SonarrReleaseProfile {Ignored = new List<string> {"one", "two", "three"}};
        var sut = new SonarrReleaseProfileCompatibilityHandler(compat, ctx.Mapper);

        var result = await sut.CompatibleReleaseProfileForSendingAsync(data);

        result.Should().BeEquivalentTo(data);
    }
}
