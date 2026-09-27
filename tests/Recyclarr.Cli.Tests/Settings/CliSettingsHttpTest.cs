using Recyclarr.Cli.Tests.Reusable;

namespace Recyclarr.Cli.Tests.Settings;

// A cli.yml the CLI cannot use must stop the command before it contacts any server, with a message
// that points at the file.
internal sealed class CliSettingsHttpTest : CliServerHttpFixture
{
    [Test]
    public async Task Unknown_setting_fails_the_command()
    {
        WriteCliSettings(
            """
            server:
              bogus_key: 1
            """
        );

        var exitCode = await RunCliAsync("list", "qualities", "radarr");

        exitCode.Should().Be(1);
        LogOutput.Should().Contain("cli.yml").And.Contain("bogus_key");
    }

    [Test]
    public async Task Relative_base_url_fails_the_command()
    {
        WriteCliSettings(
            """
            server:
              base_url: recyclarr:7982
            """
        );

        var exitCode = await RunCliAsync("list", "qualities", "radarr");

        exitCode.Should().Be(1);
        LogOutput.Should().Contain("cli.yml").And.Contain("base_url");
    }
}
