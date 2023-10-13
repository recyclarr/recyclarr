using Autofac.Core.Registration;
using Recyclarr.Cli.Pipelines.MediaNaming;
using Recyclarr.Cli.Pipelines.MediaNaming.PipelinePhases;
using Recyclarr.Common;
using Recyclarr.Config.Models;

namespace Recyclarr.Cli.IntegrationTests.Pipelines;

[TestFixture]
[Parallelizable(ParallelScope.All)]
internal class MediaNamingConfigPhaseIntegrationTest : CliIntegrationFixture
{
    private sealed record UnsupportedConfigType : ServiceConfiguration
    {
        public override SupportedServices ServiceType => (SupportedServices) 999;
    }

    [Test]
    public async Task Throw_on_unknown_config_type()
    {
        var sut = Resolve<MediaNamingConfigPhase>();
        var act = () => sut.Execute(new MediaNamingPipelineContext(), new UnsupportedConfigType {InstanceName = ""});
        await act.Should().ThrowAsync<ComponentNotRegisteredException>();
    }
}
