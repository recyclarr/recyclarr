using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Autofac;
using Autofac.Features.ResolveAnything;
using CliFx.Infrastructure;
using Common.TestLibrary;
using NSubstitute;
using NUnit.Framework;
using Serilog.Events;
using TrashLib.Startup;
using VersionControl;
using VersionControl.Wrappers;

namespace Recyclarr.TestLibrary;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class IntegrationFixture : IDisposable
{
    private readonly ILifetimeScope _container;

    protected IntegrationFixture()
    {
        var compRoot = new CompositionRoot();
        _container = compRoot.Setup(default, Console, LogEventLevel.Debug).Container
            .BeginLifetimeScope(builder =>
            {
                builder.RegisterSource<AnyConcreteTypeNotAlreadyRegisteredSource>();
                builder.RegisterInstance(Fs).As<IFileSystem>();

                RegisterMockFor<IGitRepository>(builder);
                RegisterMockFor<IGitRepositoryFactory>(builder);
                RegisterMockFor<IRepositoryStaticWrapper>(builder);
            });

        SetupMetadataJson();
    }

    private void SetupMetadataJson()
    {
        var paths = Resolve<IAppPaths>();
        var metadataFile = paths.RepoDirectory.File("metadata.json");
        Fs.AddFileFromResource(metadataFile, "metadata.json");
    }

    protected MockFileSystem Fs { get; } = new();
    protected FakeInMemoryConsole Console { get; } = new();

    private static void RegisterMockFor<T>(ContainerBuilder builder) where T : class
    {
        builder.RegisterInstance(Substitute.For<T>()).As<T>();
    }

    protected T Resolve<T>(Action<ContainerBuilder> customRegistrations) where T : notnull
    {
        var childScope = _container.BeginLifetimeScope(customRegistrations);
        return childScope.Resolve<T>();
    }

    protected T Resolve<T>() where T : notnull
    {
        return _container.Resolve<T>();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        _container.Dispose();
        Console.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
