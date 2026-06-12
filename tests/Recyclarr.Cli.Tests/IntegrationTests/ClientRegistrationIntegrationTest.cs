using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Client;

namespace Recyclarr.Cli.Tests.IntegrationTests;

internal sealed class ClientRegistrationIntegrationTest : CliIntegrationFixture
{
    [Test]
    public void RecyclarrClientFactory_resolves_from_container()
    {
        var factory = Resolve<RecyclarrClientFactory>();

        factory.Should().NotBeNull();
    }
}
