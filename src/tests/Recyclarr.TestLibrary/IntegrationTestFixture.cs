using System.IO.Abstractions;
using Autofac;
using Autofac.Features.ResolveAnything;
using Recyclarr.Common;
using Recyclarr.TestLibrary.Autofac;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Recyclarr.TestLibrary;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class IntegrationTestFixture : IDisposable
{
    private readonly Lazy<ILifetimeScope> _container;
    protected ILifetimeScope Container => _container.Value;
    protected MockFileSystem Fs { get; }
    protected TestConsole Console { get; } = new();
    protected TestableLogger Logger { get; } = new();

    protected IntegrationTestFixture()
    {
        Fs = new MockFileSystem(new MockFileSystemOptions
        {
            CreateDefaultTempDir = false
        });

        // Use Lazy because we shouldn't invoke virtual methods at construction time
        _container = new Lazy<ILifetimeScope>(() =>
        {
            var builder = new ContainerBuilder();
            RegisterTypes(builder);
            RegisterStubsAndMocks(builder);
            builder.RegisterSource<AnyConcreteTypeNotAlreadyRegisteredSource>();
            return builder.Build();
        });
    }

    /// <summary>
    /// Register "real" types (usually Module-derived classes from other projects). This call happens
    /// before
    /// RegisterStubsAndMocks().
    /// </summary>
    protected virtual void RegisterTypes(ContainerBuilder builder)
    {
    }

    /// <summary>
    /// Override registrations made in the RegisterTypes() method. This method is called after
    /// RegisterTypes().
    /// </summary>
    protected virtual void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        builder.RegisterInstance(Fs).As<IFileSystem>();
        builder.RegisterInstance(Console).As<IAnsiConsole>();
        builder.RegisterInstance(Logger).As<ILogger>();

        builder.RegisterMockFor<IEnvironment>();
    }

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

        if (!_container.IsValueCreated)
        {
            return;
        }

        _container.Value.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
