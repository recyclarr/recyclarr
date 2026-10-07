using Recyclarr.Cli.Tests.Reusable;

namespace Recyclarr.Cli.Tests.Settings;

/// <summary>
/// RECYCLARR_SERVER_URL points the CLI at a running server without editing cli.yml, so a container
/// image can bake in the address of its own server.
/// </summary>
internal sealed class ServerUrlEnvironmentHttpTest : CliServerHttpFixture
{
    private const string EnvVar = "RECYCLARR_SERVER_URL";

    [Test]
    public async Task Variable_overrides_cli_yml_base_url()
    {
        WriteCliSettings(
            """
            server:
              base_url: http://from-file:7982
            """
        );
        SetCliEnvironmentVariable(EnvVar, "http://from-env:7982");

        var exitCode = await RunCliAsync("list", "qualities", "radarr");

        exitCode.Should().Be(0);
        LogOutput.Should().Contain("http://from-env:7982/").And.NotContain("from-file");
    }

    [Test]
    public async Task Empty_variable_falls_back_to_cli_yml()
    {
        WriteCliSettings(
            """
            server:
              base_url: http://from-file:7982
            """
        );
        SetCliEnvironmentVariable(EnvVar, "");

        var exitCode = await RunCliAsync("list", "qualities", "radarr");

        exitCode.Should().Be(0);
        LogOutput.Should().Contain("http://from-file:7982/");
    }

    [Test]
    public async Task Relative_url_fails_the_command_naming_the_variable()
    {
        SetCliEnvironmentVariable(EnvVar, "recyclarr:7982");

        var exitCode = await RunCliAsync("list", "qualities", "radarr");

        exitCode.Should().Be(1);
        LogOutput.Should().Contain(EnvVar).And.NotContain("cli.yml");
    }
}
