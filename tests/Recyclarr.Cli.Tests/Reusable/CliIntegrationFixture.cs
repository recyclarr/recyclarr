using System.IO.Abstractions;
using Autofac;
using Recyclarr.Platform;

namespace Recyclarr.Cli.Tests.Reusable;

// The CLI's production composition root over an in-memory filesystem and environment. It uses no
// Recyclarr.Core test infrastructure, because the CLI does not depend on Core.
internal abstract class CliIntegrationFixture : IDisposable
{
    private readonly Lazy<IContainer> _container;

    protected MockFileSystem Fs { get; } =
        new(new MockFileSystemOptions { CreateDefaultTempDir = false });

    protected IEnvironment Env { get; } = Substitute.For<IEnvironment>();

    protected CliIntegrationFixture()
    {
        // Lazy because virtual methods must not run during construction.
        _container = new Lazy<IContainer>(() =>
        {
            var builder = new ContainerBuilder();
            CompositionRoot.Setup(builder);
            builder.RegisterInstance(Fs).As<IFileSystem>();
            builder.RegisterInstance(Env);
            RegisterStubsAndMocks(builder);
            return builder.Build();
        });
    }

    // Overrides production registrations. Runs after the stub filesystem and environment.
    protected virtual void RegisterStubsAndMocks(ContainerBuilder builder) { }

    protected T Resolve<T>()
        where T : notnull
    {
        return _container.Value.Resolve<T>();
    }

    public void Dispose()
    {
        if (_container.IsValueCreated)
        {
            _container.Value.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
