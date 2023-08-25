using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using Autofac;
using Autofac.Features.ResolveAnything;
using Recyclarr.Common;
using Recyclarr.TestLibrary;
using Recyclarr.TestLibrary.Autofac;
using Recyclarr.TrashLib.ApiServices.System;
using Recyclarr.TrashLib.Repo.VersionControl;
using Recyclarr.TrashLib.Startup;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Recyclarr.TrashLib.TestLibrary;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class TrashLibIntegrationFixture : IDisposable
{
    protected TrashLibIntegrationFixture()
    {
        Fs = new MockFileSystem(new MockFileSystemOptions
        {
            CreateDefaultTempDir = false
        });

        Paths = new AppPaths(Fs.CurrentDirectory().SubDirectory("test").SubDirectory("recyclarr"));

        _container = new Lazy<IContainer>(() =>
        {
            var builder = new ContainerBuilder();

            RegisterTypes(builder);

            builder.RegisterInstance(Fs).As<IFileSystem>();
            builder.RegisterInstance(Paths).As<IAppPaths>();
            builder.RegisterInstance(Console).As<IAnsiConsole>();
            builder.RegisterInstance(Logger).As<ILogger>();

            builder.RegisterMockFor<IGitRepository>();
            builder.RegisterMockFor<IGitRepositoryFactory>();
            builder.RegisterMockFor<IEnvironment>();
            builder.RegisterMockFor<IServiceInformation>(m =>
            {
                // By default, choose some extremely high number so that all the newest features are enabled.
                m.GetVersion(default!).ReturnsForAnyArgs(_ => new Version("99.0.0.0"));
            });

            builder.RegisterSource<AnyConcreteTypeNotAlreadyRegisteredSource>();

            return builder.Build();
        });
    }

    // ReSharper disable once VirtualMemberNeverOverridden.Global
    // ReSharper disable once UnusedParameter.Global
    protected virtual void RegisterTypes(ContainerBuilder builder)
    {
        builder.RegisterModule<TrashLibAutofacModule>();

        // Normally, the CLI's composition root registers this (because we can only do it once, and it must include
        // dependent assemblies). The TrashLib assembly does have its own mapping profiles. We register those here, but
        // not in the TrashLibAutofacModule.
    }

    // ReSharper disable MemberCanBePrivate.Global

    private readonly Lazy<IContainer> _container;
    protected ILifetimeScope Container => _container.Value;

    protected MockFileSystem Fs { get; }
    protected TestConsole Console { get; } = new();
    protected IAppPaths Paths { get; }
    protected TestableLogger Logger { get; } = new();

    // ReSharper restore MemberCanBePrivate.Global

    protected T Resolve<T>() where T : notnull
    {
        return Container.Resolve<T>();
    }

    // ReSharper disable once VirtualMemberNeverOverridden.Global
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        if (_container.IsValueCreated)
        {
            _container.Value.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
