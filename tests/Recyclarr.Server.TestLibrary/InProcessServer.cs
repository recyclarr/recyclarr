using System.IO.Abstractions.TestingHelpers;
using System.Net.Http.Headers;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Platform;
using Recyclarr.Server.Hosting;
using Recyclarr.TestLibrary;

namespace Recyclarr.Server.TestLibrary;

/// <summary>
/// Recyclarr.Server running in-process behind an in-memory test server, over an in-memory
/// filesystem and environment. It exercises the real ASP.NET Core pipeline (routing, api/v
/// version prefix, FastEndpoints middleware, Problem Details, wire JSON).
/// </summary>
/// <remarks>
/// The host is assembled by <see cref="ServerHost"/>, the same code <c>Program</c> runs, with an
/// in-memory test server instead of Kestrel. The server starts on first use and through the
/// production startup path, so startup failures are logged the same way and surface as an
/// exception here. Registrations from <see cref="Configure"/> are an extra
/// <c>ConfigureContainer</c> callback, which runs after the composition root, so they override
/// production registrations. <c>ConfigureTestServices</c> cannot substitute dependencies: Autofac
/// populates the <c>IServiceCollection</c> before any <c>ConfigureContainer</c> callback runs.
/// </remarks>
public sealed class InProcessServer : IAsyncDisposable
{
    /// <summary>
    /// The server API key the in-process server requires. Clients from <see cref="CreateClient"/>
    /// send it; a test that needs a different key or none changes the client's headers.
    /// </summary>
    public const string ApiKey = "test-server-api-key-0123456789abcdef";

    private static readonly Uri BaseAddress = new("http://localhost");

    private readonly TestStubsModule _stubs = new();
    private readonly List<HttpClient> _clients = [];
    private readonly List<Action<ContainerBuilder>> _configuration = [];
    private WebApplication? _app;
    private bool _disposed;

    public InProcessServer()
    {
        _stubs.Env.Set("RECYCLARR_API_KEY", ApiKey);

        // The host reads settings.yml from this directory before the container (and the stub
        // app paths) exist.
        _stubs.Env.Set("RECYCLARR_CONFIG_DIR", _stubs.Paths.ConfigDirectory.FullName);
    }

    public MockFileSystem Fs => _stubs.Fs;
    public IAppPaths Paths => _stubs.Paths;

    /// <summary>
    /// The server's environment. The server reads it when it starts, so set variables first.
    /// </summary>
    public TestEnvironment Env => _stubs.Env;

    /// <summary>
    /// The started server's services.
    /// </summary>
    public IServiceProvider Services => Start().Services;

    /// <summary>
    /// Adds registrations to the server's container, applied in call order after the stubs, so
    /// later ones override earlier ones. The server reads its registrations once, when it
    /// starts, so a call after the first request throws instead of being silently ignored.
    /// </summary>
    public void Configure(Action<ContainerBuilder> configure)
    {
        if (_app is not null)
        {
            throw new InvalidOperationException(
                "The in-process server has already started; configure it before the first request"
            );
        }

        _configuration.Add(configure);
    }

    /// <summary>
    /// Creates a client for the in-process server that sends the server API key.
    /// </summary>
    public HttpClient CreateClient()
    {
        return Track(Start().GetTestClient());
    }

    /// <summary>
    /// Creates a client whose requests pass through the given handlers, in order, before they
    /// reach the in-process server.
    /// </summary>
    public HttpClient CreateDefaultClient(params DelegatingHandler[] handlers)
    {
        var server = Start().GetTestServer();
        if (handlers.Length == 0)
        {
            return Track(server.CreateClient());
        }

        for (var i = handlers.Length - 1; i > 0; i--)
        {
            handlers[i - 1].InnerHandler = handlers[i];
        }

        handlers[^1].InnerHandler = server.CreateHandler();
        return Track(new HttpClient(handlers[0]));
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var client in _clients)
        {
            client.Dispose();
        }

        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    private HttpClient Track(HttpClient client)
    {
        _clients.Add(client);
        client.BaseAddress = BaseAddress;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            ApiKey
        );
        return client;
    }

    private WebApplication Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_app is not null)
        {
            return _app;
        }

        var app = ServerHost.Create(
            new ServerLaunchOptions(),
            _stubs.Fs,
            _stubs.Env.Variables,
            builder =>
            {
                builder.WebHost.UseTestServer();
                builder.Host.ConfigureContainer<ContainerBuilder>(b =>
                {
                    b.RegisterModule(_stubs);
                    b.RegisterModule<ServerSyncStubsModule>();
                    b.RegisterType<NoOpReadySignal>().As<IReadySignal>().SingleInstance();
                    foreach (var configure in _configuration)
                    {
                        configure(b);
                    }
                });
            }
        ) ?? throw new InvalidOperationException("The in-process server could not be built");
        if (!app.StartServerAsync().GetAwaiter().GetResult())
        {
            app.DisposeAsync().AsTask().GetAwaiter().GetResult();
            throw new InvalidOperationException("The in-process server failed to start");
        }

        _app = app;
        return app;
    }
}
