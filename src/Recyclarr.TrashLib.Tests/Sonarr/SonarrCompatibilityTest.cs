using System.Reactive.Linq;
using AutoFixture.NUnit3;
using AutoMapper;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.TestLibrary.AutoFixture;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.ExceptionTypes;
using Recyclarr.TrashLib.Services.Sonarr;
using Recyclarr.TrashLib.Services.Sonarr.Api;
using Recyclarr.TrashLib.Services.Sonarr.Api.Objects;
using Recyclarr.TrashLib.Services.Sonarr.Config;
using Recyclarr.TrashLib.Services.System;
using Recyclarr.TrashLib.Services.System.Dto;
using Recyclarr.TrashLib.Startup;
using Serilog;

namespace Recyclarr.TrashLib.Tests.Sonarr;

[TestFixture]
[Parallelizable(ParallelScope.All)]
public class SonarrCompatibilityTest
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
        var sut = new SonarrReleaseProfileCompatibilityHandler(Substitute.For<ILogger>(), compat, ctx.Mapper);

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
        var sut = new SonarrReleaseProfileCompatibilityHandler(Substitute.For<ILogger>(), compat, ctx.Mapper);

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
        var sut = new SonarrReleaseProfileCompatibilityHandler(Substitute.For<ILogger>(), compat, ctx.Mapper);

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
        var sut = new SonarrReleaseProfileCompatibilityHandler(Substitute.For<ILogger>(), compat, ctx.Mapper);

        var result = await sut.CompatibleReleaseProfileForSendingAsync(data);

        result.Should().BeEquivalentTo(data);
    }

    [Test, AutoMockData]
    public async Task Failure_when_release_profiles_used_with_sonarr_v4(
        [Frozen] ISystemApiService api,
        [Frozen(Matching.ImplementedInterfaces)] SonarrCompatibility compatibility,
        SonarrVersionEnforcement enforcement)
    {
        api.GetStatus().Returns(new SystemStatus("Sonarr", "4.0"));

        var config = new SonarrConfiguration
        {
            ReleaseProfiles = new List<ReleaseProfileConfig> {new()}
        };

        var act = () => enforcement.DoVersionEnforcement(config);

        await act.Should().ThrowAsync<VersionException>().WithMessage("Sonarr v4*");
    }

    [Test, AutoMockData]
    public async Task No_failure_when_release_profiles_used_with_sonarr_v3(
        [Frozen] ISystemApiService api,
        [Frozen(Matching.ImplementedInterfaces)] SonarrCompatibility compatibility,
        SonarrVersionEnforcement enforcement)
    {
        api.GetStatus().Returns(new SystemStatus("Sonarr", "3.9"));

        var config = new SonarrConfiguration
        {
            ReleaseProfiles = new List<ReleaseProfileConfig> {new()}
        };

        var act = () => enforcement.DoVersionEnforcement(config);

        await act.Should().NotThrowAsync();
    }

    [Test, AutoMockData]
    public async Task Failure_when_custom_formats_used_with_sonarr_v3(
        [Frozen] ISystemApiService api,
        [Frozen(Matching.ImplementedInterfaces)] SonarrCompatibility compatibility,
        SonarrVersionEnforcement enforcement)
    {
        api.GetStatus().Returns(new SystemStatus("Sonarr", "3.9"));

        var config = new SonarrConfiguration
        {
            CustomFormats = new List<CustomFormatConfig> {new()}
        };

        var act = () => enforcement.DoVersionEnforcement(config);

        await act.Should().ThrowAsync<VersionException>().WithMessage("Sonarr v3*");
    }

    [Test, AutoMockData]
    public async Task No_failure_when_custom_formats_used_with_sonarr_v4(
        [Frozen] ISystemApiService api,
        [Frozen(Matching.ImplementedInterfaces)] SonarrCompatibility compatibility,
        SonarrVersionEnforcement enforcement)
    {
        api.GetStatus().Returns(new SystemStatus("Sonarr", "4.0"));

        var config = new SonarrConfiguration
        {
            CustomFormats = new List<CustomFormatConfig> {new()}
        };

        var act = () => enforcement.DoVersionEnforcement(config);

        await act.Should().NotThrowAsync();
    }
}
