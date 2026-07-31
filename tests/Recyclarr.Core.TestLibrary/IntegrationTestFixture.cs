using System.IO.Abstractions.TestingHelpers;
using Autofac;
using Autofac.Extras.Ordering;
using Autofac.Features.ResolveAnything;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Platform;

namespace Recyclarr.Core.TestLibrary;

public abstract class IntegrationTestFixture : IDisposable
{
    private readonly Lazy<ILifetimeScope> _container;
    private readonly TestStubsModule _stubs = new();

    protected ILifetimeScope Container => _container.Value;
    protected MockFileSystem Fs => _stubs.Fs;
    protected IAppPaths Paths => _stubs.Paths;

    protected IntegrationTestFixture()
    {
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
    /// Register "real" types (usually Module-derived classes from other projects). This call
    /// happens before RegisterStubsAndMocks().
    /// </summary>
    protected virtual void RegisterTypes(ContainerBuilder builder)
    {
        // Needed for Autofac.Extras.Ordering
        builder.RegisterSource<OrderedRegistrationSource>();
        builder.RegisterModule<CoreAutofacModule>();
    }

    /// <summary>
    /// Override registrations made in the RegisterTypes() method. This method is called after
    /// RegisterTypes().
    /// </summary>
    protected virtual void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        builder.RegisterModule(_stubs);
    }

    protected T Resolve<T>()
        where T : notnull
    {
        return Container.Resolve<T>();
    }

    protected LifetimeScopeWrapper<T> ResolveWithConfig<T>(
        IServiceConfiguration config,
        Action<ContainerBuilder>? configure = null
    )
        where T : notnull
    {
        var childScope = Container.BeginLifetimeScope(
            "instance",
            c =>
            {
                c.RegisterInstance(config).As<IServiceConfiguration>().As(config.GetType());
                configure?.Invoke(c);
            }
        );
        return new LifetimeScopeWrapper<T>(childScope);
    }

    // ReSharper disable once VirtualMemberNeverOverridden.Global
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing || !_container.IsValueCreated)
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
