using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Recyclarr.EndToEndTests.Clients;
using TUnit.Core.Interfaces;

namespace Recyclarr.EndToEndTests.Fixtures;

internal sealed class SonarrContainer : IAsyncInitializer, IAsyncDisposable
{
    private const string ApiKey = "testkey";
    private const int ServicePort = 8989;

    public IContainer Container { get; } =
        new ContainerBuilder("linuxserver/sonarr:latest")
            .WithPortBinding(ServicePort, true)
            .WithEnvironment("SONARR__AUTH__APIKEY", ApiKey)
            .WithTmpfsMount("/config")
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(ServicePort))
            )
            .WithCleanUp(true)
            .Build();

    public ServarrTestClient Client { get; private set; } = null!;

    public string BaseUrl => $"http://localhost:{Container.GetMappedPublicPort(ServicePort)}";

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
        Client = new ServarrTestClient(BaseUrl, ApiKey);
    }

    public async ValueTask DisposeAsync()
    {
        await Container.DisposeAsync();
    }
}

internal sealed class RadarrContainer : IAsyncInitializer, IAsyncDisposable
{
    private const string ApiKey = "testkey";
    private const int ServicePort = 7878;

    public IContainer Container { get; } =
        new ContainerBuilder("linuxserver/radarr:latest")
            .WithPortBinding(ServicePort, true)
            .WithEnvironment("RADARR__AUTH__APIKEY", ApiKey)
            .WithTmpfsMount("/config")
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(ServicePort))
            )
            .WithCleanUp(true)
            .Build();

    public ServarrTestClient Client { get; private set; } = null!;

    public string BaseUrl => $"http://localhost:{Container.GetMappedPublicPort(ServicePort)}";

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
        Client = new ServarrTestClient(BaseUrl, ApiKey);
    }

    public async ValueTask DisposeAsync()
    {
        await Container.DisposeAsync();
    }
}
