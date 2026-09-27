using Recyclarr.Cli.Tests.Reusable;

namespace Recyclarr.Cli.Tests.Server;

/// <summary>
/// The CLI writes no log files, so its console log is the only local record of what it exchanged
/// with the server. That record must carry the request line and, at the most detailed level, the
/// payloads.
/// </summary>
internal sealed class HttpTrafficLogHttpTest : CliServerHttpFixture
{
    [Test]
    public async Task Server_requests_are_logged_with_their_bodies()
    {
        var exitCode = await RunCliAsync("list", "qualities", "radarr", "--log", "verbose");

        exitCode.Should().Be(0);
        LogOutput
            .Should()
            .MatchRegex(@"HTTP GET \S+/api/v1/\S+ responded 200")
            .And.Contain("HTTP response body:");
    }
}
