using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Autofac;
using Autofac.Features.ResolveAnything;
using CliFx.Infrastructure;
using NUnit.Framework;
using Serilog.Events;

namespace Recyclarr.TestLibrary;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public abstract class IntegrationFixture
{
    private readonly ILifetimeScope _container;

    protected IntegrationFixture()
    {
        var compRoot = new CompositionRoot();
        _container = compRoot.Setup(default, new FakeConsole(), LogEventLevel.Debug).Container
            .BeginLifetimeScope(builder =>
            {
                builder.RegisterSource<AnyConcreteTypeNotAlreadyRegisteredSource>();
                builder.RegisterInstance(Fs).As<IFileSystem>();
            });
    }

    protected MockFileSystem Fs { get; } = new();

    protected T Resolve<T>(Action<ContainerBuilder> customRegistrations) where T : notnull
    {
        var childScope = _container.BeginLifetimeScope(customRegistrations);
        return childScope.Resolve<T>();
    }

    protected T Resolve<T>() where T : notnull
    {
        return _container.Resolve<T>();
    }
}
