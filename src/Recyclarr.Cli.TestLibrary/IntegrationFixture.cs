using System.IO.Abstractions;
using System.IO.Abstractions.Extensions;
using System.IO.Abstractions.TestingHelpers;
using System.Reactive.Linq;
using Autofac;
using Autofac.Features.ResolveAnything;
using CliFx.Infrastructure;
using NSubstitute;
using NUnit.Framework;
using Recyclarr.Cli.Command;
using Recyclarr.Common.TestLibrary;
using Recyclarr.TestLibrary;
using Recyclarr.TrashLib;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Repo.VersionControl;
using Recyclarr.TrashLib.Services.System;
using Recyclarr.TrashLib.Startup;
using Serilog;
using Serilog.Events;

namespace Recyclarr.Cli.TestLibrary;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class IntegrationFixture : IDisposable
{
    protected IntegrationFixture()
    {
        Paths = new AppPaths(Fs.CurrentDirectory().SubDirectory("test").SubDirectory("recyclarr"));
        Logger = CreateLogger();

        Container = CompositionRoot.Setup(builder =>
        {
            builder.RegisterInstance(Fs).As<IFileSystem>();
            builder.RegisterInstance(Paths).As<IAppPaths>();
            builder.RegisterInstance(Console).As<IConsole>();
            builder.RegisterInstance(Logger).As<ILogger>().SingleInstance();

            builder.RegisterMockFor<IServiceCommand>();
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
        });

        SetupMetadataJson();
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
            .CreateLogger();
    }

    private void SetupMetadataJson()
    {
        var metadataFile = Paths.RepoDirectory.File("metadata.json");
        Fs.AddFileFromResource(metadataFile, "metadata.json");
    }

    // ReSharper disable MemberCanBePrivate.Global

    protected MockFileSystem Fs { get; } = new();
    protected FakeInMemoryConsole Console { get; } = new();
    protected ILifetimeScope Container { get; }
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

        Container.Dispose();
        Console.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
