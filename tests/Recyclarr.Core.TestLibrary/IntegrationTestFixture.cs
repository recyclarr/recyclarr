using System.IO.Abstractions.TestingHelpers;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.ResolveAnything;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Recyclarr.Config;
using Recyclarr.Config.Models;
using Recyclarr.Platform;
using Recyclarr.ResourceProviders.Infrastructure;
using Recyclarr.Settings.Configuration;
using Recyclarr.TestLibrary;

namespace Recyclarr.Core.TestLibrary;

public abstract class IntegrationTestFixture : IDisposable
{
    private readonly Lazy<ILifetimeScope> _container;
    private readonly TestStubsModule _stubs = new();

    protected ILifetimeScope Container => _container.Value;
    protected MockFileSystem Fs => _stubs.Fs;
    protected IAppPaths Paths => _stubs.Paths;
    protected TestEnvironment Env => _stubs.Env;

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
    /// Register the production composition under test. This call happens before
    /// RegisterStubsAndMocks().
    /// </summary>
    protected abstract void RegisterTypes(ContainerBuilder builder);

    /// <summary>
    /// Override registrations made in the RegisterTypes() method. This method is called after
    /// RegisterTypes().
    /// </summary>
    protected virtual void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        builder.RegisterModule(_stubs);
        builder.RegisterInstance(_stubs.Env.Configuration);

        // Read when the container is built, so tests write settings.yml before resolving.
        var settings = new ConfigurationBuilder();
        var report = settings.AddSettingsYaml(Paths.ConfigDirectory, _stubs.Env.Configuration);
        var services = new ServiceCollection();
        services.AddRecyclarrSettings(settings.Build(), report);
        builder.Populate(services);

        // Every scope reads one test-owned guide index; tests register guide files into it.
        builder.RegisterInstance(ResourceScope.For(Guide));
    }

    /// <summary>The guide data every scope in this fixture reads.</summary>
    protected GuideIndex Guide { get; } = new();

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
