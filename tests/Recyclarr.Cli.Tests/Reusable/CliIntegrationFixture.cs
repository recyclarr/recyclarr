using System.Diagnostics.CodeAnalysis;
using Autofac;
using Recyclarr.TestLibrary;

namespace Recyclarr.Cli.Tests.Reusable;

/// <summary>
/// Base for tests that run the CLI's composition root; see <see cref="CliHost"/>.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "TUnit owns the fixture's lifetime; an [After(Test)] hook disposes the field"
)]
internal abstract class CliIntegrationFixture
{
    private readonly CliHost _host;

    protected CliIntegrationFixture()
    {
        // The host calls back only when it builds the container, after construction.
        _host = new CliHost(builder => RegisterStubsAndMocks(builder));
    }

    protected MockFileSystem Fs => _host.Fs;
    protected TestEnvironment Env => _host.Env;

    /// <summary>
    /// Overrides production registrations. Runs after the stub filesystem and environment.
    /// </summary>
    protected virtual void RegisterStubsAndMocks(ContainerBuilder builder) { }

    protected T Resolve<T>()
        where T : notnull
    {
        return _host.Resolve<T>();
    }

    [After(Test)]
    public void DisposeHost()
    {
        _host.Dispose();
    }
}
