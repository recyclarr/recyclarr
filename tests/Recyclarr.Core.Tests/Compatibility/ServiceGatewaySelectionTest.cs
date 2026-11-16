using Autofac;
using Autofac.Core;
using Recyclarr.Compatibility;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Pipelines;
using Recyclarr.Servarr.CustomFormat;
using RadarrApi = Recyclarr.Api.Radarr;
using SonarrApi = Recyclarr.Api.Sonarr.V3;
using SonarrV5Api = Recyclarr.Api.Sonarr.V5;

namespace Recyclarr.Core.Tests.Compatibility;

internal sealed class ServiceGatewaySelectionTest : CoreIntegrationTestFixture
{
    private readonly SonarrApi.ICustomFormatApi _sonarrApi =
        Substitute.For<SonarrApi.ICustomFormatApi>();

    private readonly SonarrV5Api.ICustomFormatApi _sonarrV5Api =
        Substitute.For<SonarrV5Api.ICustomFormatApi>();

    private readonly RadarrApi.ICustomFormatApi _radarrApi =
        Substitute.For<RadarrApi.ICustomFormatApi>();

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);
        builder.RegisterInstance(_sonarrApi).As<SonarrApi.ICustomFormatApi>();
        builder.RegisterInstance(_sonarrV5Api).As<SonarrV5Api.ICustomFormatApi>();
        builder.RegisterInstance(_radarrApi).As<RadarrApi.ICustomFormatApi>();
    }

    [Test]
    public async Task Sonarr_v5_target_uses_sonarr_v5_api()
    {
        using var instance = ResolveWithConfig<ILifetimeScope>(NewConfig.Sonarr());
        await using var detected = instance.Entry.BeginDetectedServiceScope(
            new DetectedService(new Version(5, 0), ServiceApiTarget.SonarrV5)
        );

        await detected.Resolve<ICustomFormatService>().GetCustomFormats(CancellationToken.None);

        await _sonarrV5Api.ReceivedWithAnyArgs().CustomformatGet();
        await _sonarrApi.DidNotReceiveWithAnyArgs().CustomformatGet();
    }

    [Test]
    public async Task Sonarr_v3_target_uses_sonarr_v3_api()
    {
        using var instance = ResolveWithConfig<ILifetimeScope>(NewConfig.Sonarr());
        await using var detected = instance.Entry.BeginDetectedServiceScope(
            new DetectedService(new Version(4, 0), ServiceApiTarget.SonarrV3)
        );

        await detected.Resolve<ICustomFormatService>().GetCustomFormats(CancellationToken.None);

        await _sonarrApi.ReceivedWithAnyArgs().CustomformatGet();
        await _radarrApi.DidNotReceiveWithAnyArgs().CustomformatGet();
    }

    [Test]
    public async Task Radarr_v3_target_uses_radarr_api()
    {
        using var instance = ResolveWithConfig<ILifetimeScope>(NewConfig.Radarr());
        await using var detected = instance.Entry.BeginDetectedServiceScope(
            new DetectedService(new Version(5, 0), ServiceApiTarget.RadarrV3)
        );

        await detected.Resolve<ICustomFormatService>().GetCustomFormats(CancellationToken.None);

        await _radarrApi.ReceivedWithAnyArgs().CustomformatGet();
        await _sonarrApi.DidNotReceiveWithAnyArgs().CustomformatGet();
    }

    // Sonarr v5 has no quality definition gateway, so building the pipeline executor must not
    // require one.
    [TestCase(ServiceApiTarget.SonarrV3)]
    [TestCase(ServiceApiTarget.SonarrV5)]
    public void Pipeline_executor_resolves_for_every_sonarr_target(ServiceApiTarget target)
    {
        using var instance = ResolveWithConfig<ILifetimeScope>(NewConfig.Sonarr());
        using var detected = instance.Entry.BeginDetectedServiceScope(
            new DetectedService(new Version(5, 0), target)
        );

        var act = () => detected.Resolve<IPipelineExecutor>();

        act.Should().NotThrow();
    }

    [Test]
    public void Gateway_cannot_be_resolved_before_detection()
    {
        using var instance = ResolveWithConfig<ILifetimeScope>(NewConfig.Sonarr());

        var act = () => instance.Entry.Resolve<ICustomFormatService>();

        act.Should().Throw<DependencyResolutionException>();
    }
}
