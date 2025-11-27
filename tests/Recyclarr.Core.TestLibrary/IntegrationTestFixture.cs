using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Autofac;
using Autofac.Extras.Ordering;
using Autofac.Features.ResolveAnything;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Compatibility;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.TestLibrary;
using Recyclarr.TestLibrary.Autofac;
using Recyclarr.VersionControl;
using Serilog;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Recyclarr.Core.TestLibrary;

public abstract class IntegrationTestFixture : IDisposable
{
    private readonly Lazy<ILifetimeScope> _container;
    protected ILifetimeScope Container => _container.Value;
    protected MockFileSystem Fs { get; }
    protected TestConsole Console { get; } = new();
    protected TestableLogger Logger { get; } = new();
    protected IAppPaths Paths => Resolve<IAppPaths>();

    protected IntegrationTestFixture()
    {
        Fs = new MockFileSystem(new MockFileSystemOptions { CreateDefaultTempDir = false });

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
        builder.RegisterModule<CoreAutofacModule>();
    }

    /// <summary>
    /// Override registrations made in the RegisterTypes() method. This method is called after
    /// RegisterTypes().
    /// </summary>
    protected virtual void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        builder.RegisterInstance(Fs).As<IFileSystem>().AsSelf();
        builder.RegisterInstance(Console).As<IAnsiConsole>();
        builder.RegisterInstance(Logger).As<ILogger>();

        builder.RegisterType<StubRepoUpdater>().As<IRepoUpdater>().SingleInstance();

        builder.RegisterMockFor<IEnvironment>();
        builder.RegisterMockFor<IGitRepository>();
        builder.RegisterMockFor<IGitRepositoryFactory>();
        builder.RegisterMockFor<IServiceInformation>(m =>
        {
            // By default, choose some extremely high number so that all the newest features are enabled.
            m.GetVersion(CancellationToken.None).ReturnsForAnyArgs(_ => new Version("99.0.0.0"));
        });

        // Create empty settings.yml to avoid SettingsLoader creating one and triggering YAML errors
        Fs.AddFile(
            Fs.Path.Combine(
                Fs.CurrentDirectory().SubDirectory("test").SubDirectory("recyclarr").FullName,
                "settings.yml"
            ),
            new MockFileData("# Empty settings for tests\n")
        );
    }

    [SetUp]
    public void Setup()
    {
        var appDataSetup = Resolve<DefaultAppDataSetup>();
        appDataSetup.SetAppDataDirectoryOverride(
            Fs.CurrentDirectory().SubDirectory("test").SubDirectory("recyclarr").FullName
        );
    }

    [TearDown]
    public void Teardown()
    {
        System.Console.Write(Console.Output);
    }

    protected T Resolve<T>()
        where T : notnull
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
