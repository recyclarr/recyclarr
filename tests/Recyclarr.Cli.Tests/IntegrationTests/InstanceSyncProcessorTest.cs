using Autofac;
using Recyclarr.Cli.Console.Settings;
using Recyclarr.Cli.Processors.Sync;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Compatibility;
using Recyclarr.Config.Models;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Servarr.CustomFormat;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class InstanceSyncProcessorTest : CliIntegrationFixture
{
    [Test]
    public async Task Incompatible_service_fails_instance_without_touching_gateways()
    {
        var serviceInfo = Substitute.For<IServiceInformation>();
        serviceInfo.GetAppName(default).ReturnsForAnyArgs("Sonarr");
        serviceInfo.GetVersion(default).ReturnsForAnyArgs(new Version(3, 0));
        var customFormats = Substitute.For<ICustomFormatService>();

        await using var syncScope = Container.BeginLifetimeScope("sync");
        await using var instanceScope = syncScope.BeginLifetimeScope(
            "instance",
            c =>
            {
                c.RegisterInstance(NewConfig.Sonarr()).As<IServiceConfiguration>();
                c.RegisterInstance(serviceInfo).As<IServiceInformation>();
                c.RegisterInstance(customFormats).As<ICustomFormatService>();
            }
        );

        var result = await instanceScope
            .Resolve<InstanceSyncProcessor>()
            .Process(Substitute.For<ISyncSettings>(), CancellationToken.None);

        result.Should().Be(InstanceSyncResult.Failed);
        customFormats.ReceivedCalls().Should().BeEmpty();
    }
}
