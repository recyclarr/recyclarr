using AutoMapper;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.TrashLib.Services.Sonarr;
using Recyclarr.TrashLib.Services.Sonarr.Api;
using Recyclarr.TrashLib.Services.Sonarr.Api.Objects;
using Recyclarr.TrashLib.Startup;
using Serilog;

namespace Recyclarr.TrashLib.Tests.Sonarr;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SonarrCompatibilityTest
{
    private sealed class TestContext : IDisposable
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

        static SonarrCapabilities Compat() => new();
        var dataV1 = new SonarrReleaseProfileV1 {Ignored = "one,two,three"};
        var sut = new SonarrReleaseProfileCompatibilityHandler(Substitute.For<ILogger>(), Compat, ctx.Mapper);

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

        static SonarrCapabilities Compat() => new();
        var dataV2 = new SonarrReleaseProfile {Ignored = new List<string> {"one", "two", "three"}};
        var sut = new SonarrReleaseProfileCompatibilityHandler(Substitute.For<ILogger>(), Compat, ctx.Mapper);

        var result = sut.CompatibleReleaseProfileForReceiving(JObject.Parse(ctx.SerializeJson(dataV2)));

        result.Should().BeEquivalentTo(dataV2);
    }

    [Test]
    public void Send_v2_to_v1()
    {
        using var ctx = new TestContext();

        static SonarrCapabilities Compat() => new() {ArraysNeededForReleaseProfileRequiredAndIgnored = false};

        var data = new SonarrReleaseProfile {Ignored = new List<string> {"one", "two", "three"}};
        var sut = new SonarrReleaseProfileCompatibilityHandler(Substitute.For<ILogger>(), Compat, ctx.Mapper);

        var result = sut.CompatibleReleaseProfileForSending(data);

        result.Should().BeEquivalentTo(new SonarrReleaseProfileV1 {Ignored = "one,two,three"});
    }

    [Test]
    public void Send_v2_to_v2()
    {
        using var ctx = new TestContext();

        static SonarrCapabilities Compat() => new() {ArraysNeededForReleaseProfileRequiredAndIgnored = true};

        var data = new SonarrReleaseProfile {Ignored = new List<string> {"one", "two", "three"}};
        var sut = new SonarrReleaseProfileCompatibilityHandler(Substitute.For<ILogger>(), Compat, ctx.Mapper);

        var result = sut.CompatibleReleaseProfileForSending(data);

        result.Should().BeEquivalentTo(data);
    }
}
