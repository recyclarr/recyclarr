using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.IO.Abstractions.TestingHelpers;
using System.Reactive.Linq;
using Autofac;
using Autofac.Features.ResolveAnything;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Common.TestLibrary;
using Recyclarr.TestLibrary;
using Recyclarr.TrashLib;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Repo.VersionControl;
using Recyclarr.TrashLib.Services.System;
using Recyclarr.TrashLib.Startup;
using Serilog;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Testing;

namespace Recyclarr.Cli.TestLibrary;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class IntegrationFixture : IDisposable
{
    protected IntegrationFixture()
    {
        Paths = new AppPaths(Fs.CurrentDirectory().SubDirectory("test").SubDirectory("recyclarr"));
        Logger = CreateLogger();

        SetupMetadataJson();

        _container = new Lazy<IContainer>(() =>
        {
            var builder = new ContainerBuilder();
            CompositionRoot.Setup(builder);

            builder.RegisterInstance(Fs).As<IFileSystem>();
            builder.RegisterInstance(Paths).As<IAppPaths>();
            builder.RegisterInstance(Console).As<IAnsiConsole>();
            builder.RegisterInstance(Logger).As<ILogger>();

            builder.RegisterMockFor<IGitRepository>();
            builder.RegisterMockFor<IGitRepositoryFactory>();
            builder.RegisterMockFor<IServiceConfiguration>();
            builder.RegisterMockFor<IServiceInformation>(m =>
            {
                // By default, choose some extremely high number so that all the newest features are enabled.
                m.Version.Returns(_ => Observable.Return(new Version("99.0.0.0")));
            });

            RegisterExtraTypes(builder);

            builder.RegisterSource<AnyConcreteTypeNotAlreadyRegisteredSource>();

            return builder.Build();
        });
    }

    // ReSharper disable once VirtualMemberNeverOverridden.Global
    // ReSharper disable once UnusedParameter.Global
    protected virtual void RegisterExtraTypes(ContainerBuilder builder)
    {
    }

    private static ILogger CreateLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Debug)
            .WriteTo.TestCorrelator()
            .WriteTo.Console()
            .CreateLogger();
    }

    private void SetupMetadataJson()
    {
        var metadataFile = Paths.RepoDirectory.File("metadata.json");
        Fs.AddFileFromResource(metadataFile, "metadata.json");
    }

    // ReSharper disable MemberCanBePrivate.Global

    private readonly Lazy<IContainer> _container;
    protected ILifetimeScope Container => _container.Value;

    protected MockFileSystem Fs { get; } = new();
    protected TestConsole Console { get; } = new();
    protected IAppPaths Paths { get; }
    protected ILogger Logger { get; }

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
