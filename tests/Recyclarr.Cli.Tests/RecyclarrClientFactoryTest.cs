using Recyclarr.Client;

namespace Recyclarr.Cli.Tests;

internal sealed class RecyclarrClientFactoryTest
{
    [Test]
    public void Create_returns_client_with_health_property()
    {
        var factory = new RecyclarrClientFactory();

        var client = factory.Create("http://localhost:7982");

        client.V1.Health.Should().NotBeNull();
    }
}
