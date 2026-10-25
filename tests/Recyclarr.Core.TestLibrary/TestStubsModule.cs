using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Autofac;
using NSubstitute;
using Recyclarr.Compatibility;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.TestLibrary;
using Recyclarr.TestLibrary.Autofac;
using Recyclarr.VersionControl;
using Serilog;
using Spectre.Console;

namespace Recyclarr.Core.TestLibrary;

/// <summary>
/// The standard set of test adapters for ports that would otherwise reach the network, the real
/// filesystem, or the console, together with the in-memory environment they operate against.
/// Register this last so it overrides the production registrations it replaces.
/// </summary>
/// <remarks>
/// This is a module rather than a fixture base class because <c>WebApplicationFactory</c>-derived
/// fixtures cannot inherit from <see cref="IntegrationTestFixture"/>.
/// </remarks>
public sealed class TestStubsModule : Module
{
    public MockFileSystem Fs { get; } =
        new(new MockFileSystemOptions { CreateDefaultTempDir = false });

    public IAppPaths Paths { get; }

    /// <summary>
    /// The process environment the container sees. Fixtures configure variables on it.
    /// </summary>
    public IEnvironment Env { get; } = Substitute.For<IEnvironment>();

    public TestStubsModule()
    {
        Env.GetFolderPath(Arg.Any<Environment.SpecialFolder>()).Returns("/mock/home");

        var testRoot = Fs.CurrentDirectory().SubDirectory("test").SubDirectory("recyclarr");
        var paths = new AppPaths(testRoot, testRoot, testRoot.SubDirectory("state"));
        paths.CreateWritableDirectories();
        Paths = paths;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterInstance(Fs).As<IFileSystem>().AsSelf();
        builder.RegisterInstance(Paths);

        builder.Register(_ => NUnitAnsiConsole.Create()).As<IAnsiConsole>().SingleInstance();
        builder.RegisterType<TestableLogger>().As<ILogger>().SingleInstance();
        builder.RegisterType<StubRepoUpdater>().As<IRepoUpdater>().SingleInstance();
        builder.RegisterType<BlockedHttpClientFactory>().As<IHttpClientFactory>().SingleInstance();

        builder.RegisterInstance(Env);

        builder.RegisterMockFor<IGitRepository>();

        builder.RegisterMockFor<IServiceInformation>(m =>
        {
            // By default, choose some extremely high number so that all the newest features are enabled.
            m.GetVersion(CancellationToken.None).ReturnsForAnyArgs(_ => new Version("99.0.0.0"));
        });
    }
}
