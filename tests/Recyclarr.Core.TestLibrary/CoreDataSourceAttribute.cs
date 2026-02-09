using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Autofac;
using Autofac.Extras.Ordering;
using Autofac.Features.ResolveAnything;
using NSubstitute;
using Recyclarr.Compatibility;
using Recyclarr.Config.Filtering;
using Recyclarr.Platform;
using Recyclarr.Repo;
using Recyclarr.TestLibrary;
using Recyclarr.TestLibrary.Autofac;
using Recyclarr.VersionControl;
using Serilog;
using Spectre.Console;

namespace Recyclarr.Core.TestLibrary;

/// <summary>
/// TUnit data source attribute that provides Autofac DI for integration tests.
/// Registers CoreAutofacModule plus common test stubs and mocks.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1813:Avoid unsealed attributes",
    Justification = "Intentionally inheritable for CliDataSourceAttribute"
)]
public class CoreDataSourceAttribute : DependencyInjectionDataSourceAttribute<ILifetimeScope>
{
    public override ILifetimeScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var builder = new ContainerBuilder();

        builder
            .Register(_ => new MockFileSystem(
                new MockFileSystemOptions { CreateDefaultTempDir = false }
            ))
            .As<IFileSystem>()
            .AsSelf()
            .SingleInstance();

        RegisterTypes(builder);
        RegisterStubsAndMocks(builder);

        builder.RegisterSource<AnyConcreteTypeNotAlreadyRegisteredSource>();

        return builder.Build();
    }

    public override object? Create(ILifetimeScope scope, Type type)
    {
        return scope.ResolveOptional(type);
    }

    /// <summary>
    /// Register production types (Autofac modules). Called before RegisterStubsAndMocks().
    /// </summary>
    protected virtual void RegisterTypes(ContainerBuilder builder)
    {
        builder.RegisterSource<OrderedRegistrationSource>();
        builder.RegisterModule<CoreAutofacModule>();
    }

    /// <summary>
    /// Register test doubles. Called after RegisterTypes() to allow overriding production registrations.
    /// </summary>
    protected virtual void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        builder.Register(_ => TestAnsiConsole.Create()).As<IAnsiConsole>().SingleInstance();
        builder.RegisterType<TestableLogger>().As<ILogger>().SingleInstance();
        builder.RegisterType<StubRepoUpdater>().As<IRepoUpdater>().SingleInstance();

        builder.RegisterMockFor<IEnvironment>(m =>
        {
            m.GetFolderPath(Arg.Any<Environment.SpecialFolder>()).Returns("/mock/home");
        });
        builder.RegisterMockFor<IGitRepository>();
        builder.RegisterMockFor<IFilterResultRenderer>();
        builder.RegisterMockFor<IServiceInformation>(m =>
        {
            m.GetVersion(CancellationToken.None).ReturnsForAnyArgs(_ => new Version("99.0.0.0"));
        });

        builder
            .Register(c =>
            {
                var fs = c.Resolve<MockFileSystem>();
                var testRoot = fs.CurrentDirectory().SubDirectory("test").SubDirectory("recyclarr");
                var paths = new AppPaths(testRoot, testRoot);
                paths.CreateTopDirectories();

                // Create empty settings.yml to avoid SettingsLoader errors
                fs.AddFile(
                    fs.Path.Combine(testRoot.FullName, "settings.yml"),
                    new MockFileData("# Empty settings for tests\n")
                );

                return paths;
            })
            .As<IAppPaths>()
            .SingleInstance();
    }
}
