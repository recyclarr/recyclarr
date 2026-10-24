using System.IO.Abstractions.TestingHelpers;
using System.Net.Http.Headers;
using Autofac;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Platform;
using Recyclarr.Server.Hosting;

namespace Recyclarr.Server.TestLibrary;

/// <summary>
/// HTTP-level counterpart to <c>ServerIntegrationFixture</c>: exercises the real ASP.NET Core
/// pipeline (routing, api/v version prefix, FastEndpoints middleware, Problem Details, wire JSON)
/// rather than invoking endpoint <c>HandleAsync()</c> directly.
/// </summary>
/// <remarks>
/// <c>ConfigureTestServices</c> cannot be used to substitute dependencies here: Autofac populates
/// the <c>IServiceCollection</c> before running any <c>ConfigureContainer</c> callback, so
/// CompositionRoot's registrations always win over it. Appending another <c>ConfigureContainer</c>
/// is the seam that works, because <c>WebApplicationFactory</c> replays it after Program.cs has
/// queued its own.
/// </remarks>
public abstract class ServerHttpFixture : WebApplicationFactory<Program>
{
    /// <summary>
    /// The server API key the in-process server requires. Clients from <c>CreateClient</c> send
    /// it; a test that needs a different key or none changes the client's headers.
    /// </summary>
    public const string ApiKey = "test-server-api-key-0123456789abcdef";

    private readonly TestStubsModule _stubs;

    protected ServerHttpFixture()
    {
        _stubs = new TestStubsModule();
        _stubs.Env.GetEnvironmentVariable("RECYCLARR_API_KEY").Returns(ApiKey);
    }

    protected MockFileSystem Fs => _stubs.Fs;
    protected IAppPaths Paths => _stubs.Paths;
    protected IEnvironment Env => _stubs.Env;

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            ApiKey
        );
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureContainer<ContainerBuilder>(b =>
        {
            b.RegisterModule(_stubs);
            b.RegisterModule<ServerSyncStubsModule>();
            b.RegisterType<NoOpReadySignal>().As<IReadySignal>().SingleInstance();

            // Registered last so a derived fixture's substitutes override everything above.
            RegisterStubsAndMocks(b);
        });

        return base.CreateHost(builder);
    }

    protected virtual void RegisterStubsAndMocks(ContainerBuilder builder) { }
}
