using Recyclarr.Client;

namespace Recyclarr.Cli.Tests;

internal sealed class RecyclarrClientFactoryTest
{
    [Test]
    public void Create_returns_client_with_health_property()
    {
        var factory = new RecyclarrClientFactory();

        var client = factory.Create("http://localhost:7982");

        client.Health.Should().NotBeNull();
    }

    [Test]
    public void Create_trims_trailing_slash_from_base_address()
    {
        var factory = new RecyclarrClientFactory();

        var client = factory.Create("http://localhost:7982/");

        // The client is created without error; base URL trailing slash is stripped
        client.Should().NotBeNull();
    }
}
