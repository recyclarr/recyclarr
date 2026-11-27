using System.IO.Abstractions;
using Autofac;
using Recyclarr.TestLibrary;

namespace Recyclarr.Cli.Tests.Reusable;

/// <summary>
/// The CLI's production composition root over an in-memory filesystem and environment. It uses no
/// Recyclarr.Core test infrastructure, because the CLI does not depend on Core.
/// </summary>
/// <remarks>
/// The container is built on first resolve, so callers set up files and environment variables
/// first. <paramref name="registerStubsAndMocks"/> runs after the stub filesystem and environment
/// are registered, so its registrations override them and production ones.
/// </remarks>
internal sealed class CliHost(Action<ContainerBuilder> registerStubsAndMocks) : IDisposable
{
    private IContainer? _container;

    public MockFileSystem Fs { get; } =
        new(new MockFileSystemOptions { CreateDefaultTempDir = false });

    public TestEnvironment Env { get; } = new();

    public T Resolve<T>()
        where T : notnull
    {
        return Container().Resolve<T>();
    }

    public void Dispose()
    {
        _container?.Dispose();
    }

    private IContainer Container()
    {
        if (_container is not null)
        {
            return _container;
        }

        var builder = new ContainerBuilder();
        CompositionRoot.Setup(builder);
        builder.RegisterInstance(Fs).As<IFileSystem>();
        builder.RegisterInstance(Env.Configuration);
        registerStubsAndMocks(builder);
        _container = builder.Build();
        return _container;
    }
}
