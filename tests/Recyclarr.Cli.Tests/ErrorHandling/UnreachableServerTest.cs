using Recyclarr.Cli.ErrorHandling;
using Recyclarr.Cli.Server;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Client.V1;

namespace Recyclarr.Cli.Tests.ErrorHandling;

/// <summary>
/// A real HTTP client against a port nothing listens on, so the failure is Refit's own transport
/// error rather than a simulated one.
/// </summary>
internal sealed class UnreachableServerTest : CliIntegrationFixture
{
    [Test]
    public async Task Unreachable_server_is_reported_as_unreachable()
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri("http://127.0.0.1:9");
        await using var connection = new ServerConnection(client, ownedServer: null);

        var call = async () =>
            (await connection.Guide.Qualities(SupportedServices.Radarr)).ContentOrThrow();
        var failure = (await call.Should().ThrowAsync<Exception>()).Which;

        var handled = await Resolve<ExceptionHandler>().TryHandleAsync(failure);

        handled.Should().BeTrue();
    }
}
