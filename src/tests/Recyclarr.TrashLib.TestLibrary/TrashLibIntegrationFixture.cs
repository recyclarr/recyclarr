using System.IO.Abstractions;
using Autofac;
using Recyclarr.TestLibrary;
using Recyclarr.TestLibrary.Autofac;
using Recyclarr.TrashLib.ApiServices.System;
using Recyclarr.TrashLib.Repo.VersionControl;
using Recyclarr.TrashLib.Startup;

namespace Recyclarr.TrashLib.TestLibrary;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class TrashLibIntegrationFixture : IntegrationTestFixture
{
    protected IAppPaths Paths { get; }

    protected TrashLibIntegrationFixture()
    {
        Paths = new AppPaths(Fs.CurrentDirectory().SubDirectory("test").SubDirectory("recyclarr"));
    }

    protected override void RegisterTypes(ContainerBuilder builder)
    {
        base.RegisterTypes(builder);
        builder.RegisterModule<TrashLibAutofacModule>();
    }

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);

        builder.RegisterInstance(Paths).As<IAppPaths>();

        builder.RegisterMockFor<IGitRepository>();
        builder.RegisterMockFor<IGitRepositoryFactory>();
        builder.RegisterMockFor<IServiceInformation>(m =>
        {
            // By default, choose some extremely high number so that all the newest features are enabled.
            m.GetVersion(default!).ReturnsForAnyArgs(_ => new Version("99.0.0.0"));
        });
    }
}
