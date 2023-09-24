using System.IO.Abstractions;
using Autofac;
using Autofac.Extras.Ordering;
using Autofac.Features.ResolveAnything;
using Recyclarr.Common;
using Recyclarr.Compatibility;
using Recyclarr.Config;
using Recyclarr.Json;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.ServarrApi;
using Recyclarr.Settings;
using Recyclarr.TestLibrary;
using Recyclarr.TestLibrary.Autofac;
using Recyclarr.TrashGuide;
using Recyclarr.VersionControl;
using Recyclarr.Yaml;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Recyclarr.IntegrationTests;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class IntegrationTestFixture : IDisposable
{
    private readonly Lazy<ILifetimeScope> _container;
    protected ILifetimeScope Container => _container.Value;
    protected MockFileSystem Fs { get; }
    protected TestConsole Console { get; } = new();
    protected TestableLogger Logger { get; } = new();
    protected IAppPaths Paths { get; }

    protected IntegrationTestFixture()
    {
        Fs = new MockFileSystem(new MockFileSystemOptions
        {
            CreateDefaultTempDir = false
        });

        Paths = new AppPaths(Fs.CurrentDirectory().SubDirectory("test").SubDirectory("recyclarr"));

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
        // Needed for Autofac.Extras.Ordering
        builder.RegisterSource<OrderedRegistrationSource>();

        builder.RegisterModule<ConfigAutofacModule>();
        builder.RegisterModule<GuideAutofacModule>();
        builder.RegisterModule<YamlAutofacModule>();
        builder.RegisterModule<SettingsAutofacModule>();
        builder.RegisterModule<ApiServicesAutofacModule>();
        builder.RegisterModule<VersionControlAutofacModule>();
        builder.RegisterModule<RepoAutofacModule>();
        builder.RegisterModule<CompatibilityAutofacModule>();
        builder.RegisterModule<JsonAutofacModule>();
        builder.RegisterModule<PlatformAutofacModule>();
        builder.RegisterModule<CommonAutofacModule>();
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
        builder.RegisterInstance(Paths).As<IAppPaths>();

        builder.RegisterMockFor<IEnvironment>();
        builder.RegisterMockFor<IGitRepository>();
        builder.RegisterMockFor<IGitRepositoryFactory>();
        builder.RegisterMockFor<IServiceInformation>(m =>
        {
            // By default, choose some extremely high number so that all the newest features are enabled.
            m.GetVersion(default!).ReturnsForAnyArgs(_ => new Version("99.0.0.0"));
        });
    }

    protected T Resolve<T>() where T : notnull
    {
        return Container.Resolve<T>();
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
