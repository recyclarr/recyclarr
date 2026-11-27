using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions.TestingHelpers;
using Autofac;
using Recyclarr.Platform;
using Recyclarr.TestLibrary;

namespace Recyclarr.Server.TestLibrary;

/// <summary>
/// Base for tests that exercise the server over HTTP, rather than invoking endpoint
/// <c>HandleAsync()</c> directly as <c>ServerIntegrationFixture</c> does. Each test gets its own
/// <see cref="InProcessServer"/>.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1001:Types that own disposable fields should be disposable",
    Justification = "TUnit owns the fixture's lifetime; an [After(Test)] hook disposes the field"
)]
public abstract class ServerHttpFixture
{
    /// <inheritdoc cref="InProcessServer.ApiKey"/>
    public const string ApiKey = InProcessServer.ApiKey;

    private readonly InProcessServer _server = new();

    protected ServerHttpFixture()
    {
        // The server invokes the delegate when it starts, after construction.
        _server.Configure(RegisterStubsAndMocks);
    }

    protected MockFileSystem Fs => _server.Fs;
    protected IAppPaths Paths => _server.Paths;

    /// <inheritdoc cref="InProcessServer.Env"/>
    protected TestEnvironment Env => _server.Env;

    /// <inheritdoc cref="InProcessServer.Services"/>
    protected IServiceProvider Services => _server.Services;

    /// <inheritdoc cref="InProcessServer.CreateClient"/>
    protected HttpClient CreateClient()
    {
        var client = _server.CreateClient();
        ConfigureClient(client);
        return client;
    }

    /// <inheritdoc cref="InProcessServer.CreateDefaultClient"/>
    protected HttpClient CreateDefaultClient(params DelegatingHandler[] handlers)
    {
        var client = _server.CreateDefaultClient(handlers);
        ConfigureClient(client);
        return client;
    }

    /// <summary>
    /// Adjusts every client this fixture creates, after the server's defaults are applied.
    /// </summary>
    protected virtual void ConfigureClient(HttpClient client) { }

    /// <summary>
    /// Overrides production registrations for every test of the class.
    /// </summary>
    protected virtual void RegisterStubsAndMocks(ContainerBuilder builder) { }

    /// <summary>
    /// Adds registrations for one test's server, applied after <see cref="RegisterStubsAndMocks"/>
    /// so they override it. A call after the first request throws.
    /// </summary>
    protected void ConfigureServer(Action<ContainerBuilder> configure)
    {
        _server.Configure(configure);
    }

    [After(Test)]
    public async Task StopServer()
    {
        await _server.DisposeAsync();
    }
}
