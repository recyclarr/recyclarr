using Recyclarr.Cli.Tests.Reusable;

namespace Recyclarr.Cli.Tests.Console;

/// <summary>
/// Recyclarr 8 command lines removed in Recyclarr 9 fail with the replacement and a link to the
/// upgrade guide, before any server is contacted.
/// </summary>
internal sealed class RemovedCommandsHttpTest : CliServerHttpFixture
{
    [TestCase("#config-commands-removed", "config", "create")]
    [TestCase("#config-commands-removed", "config", "create", "-t", "x", "--force")]
    [TestCase("#config-commands-removed", "config", "list", "local")]
    [TestCase("#config-commands-removed", "config", "list", "templates", "-i")]
    [TestCase("#migrate-removed", "migrate")]
    [TestCase("#state-repair-removed", "state", "repair", "--adopt")]
    [TestCase("#sync-config-removed", "sync", "--config", "a.yml")]
    [TestCase("#sync-config-removed", "sync", "radarr", "-c", "a.yml")]
    [TestCase("#sync-config-removed", "sync", "--config=a.yml")]
    public async Task Removed_command_line_fails_with_guidance(string anchor, params string[] args)
    {
        var exitCode = await RunCliAsync(args);

        exitCode.Should().Be(1);
        LogOutput.Should().Contain("[REMOVED]").And.Contain(anchor);
        LogOutput.Should().NotContain("http://localhost/");
    }

    [Test]
    public async Task Supported_command_line_is_not_reported_as_removed()
    {
        var exitCode = await RunCliAsync("list", "qualities", "radarr");

        exitCode.Should().Be(0);
        LogOutput.Should().NotContain("[REMOVED]");
    }
}
