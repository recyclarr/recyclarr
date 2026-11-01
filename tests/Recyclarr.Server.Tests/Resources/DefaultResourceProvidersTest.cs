using Autofac;
using Recyclarr.Repo;
using Recyclarr.Server.Resources;
using Recyclarr.Server.Tests.Reusable;
using Recyclarr.VersionControl;

namespace Recyclarr.Server.Tests.Resources;

internal sealed class DefaultResourceProvidersTest : ServerIntegrationFixture
{
    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);
        builder.RegisterType<FakeGitRemote>().As<IRepoUpdater>().AsSelf().SingleInstance();
        builder.RegisterType<FakeGitRemote.Repository>().As<IGitRepository>();
    }

    [Test]
    public async Task Startup_without_settings_fetches_only_the_official_guide()
    {
        var remote = Resolve<FakeGitRemote>();
        remote.Commit(
            "trash-guides",
            new Dictionary<string, string> { ["metadata.json"] = """{ "json_paths": {} }""" }
        );

        await Resolve<ResourceCoordinator>()
            .RefreshAsync(RefreshMode.Startup, CancellationToken.None);

        remote.Fetches("trash-guides").Should().Be(1);
        remote.Fetches("config-templates").Should().Be(0);
    }
}
