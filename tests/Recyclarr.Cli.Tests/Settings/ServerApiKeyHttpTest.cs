using Recyclarr.Cli.Tests.Reusable;

namespace Recyclarr.Cli.Tests.Settings;

/// <summary>
/// The CLI sends the server API key from cli.yml, or from RECYCLARR_API_KEY when that is set, so
/// a container can share one variable between the server and the CLI (ADR-027).
/// </summary>
internal sealed class ServerApiKeyHttpTest : CliServerHttpFixture
{
    private const string EnvVar = "RECYCLARR_API_KEY";

    private void WriteKey(string? key)
    {
        var keyLine = key is null ? "" : $"\n  api_key: {key}";
        WriteCliSettings($"server:\n  base_url: http://localhost{keyLine}\n");
    }

    [Test]
    public async Task Key_from_cli_yml_is_accepted()
    {
        WriteKey(ApiKey);

        var exitCode = await RunCliAsync("list", "qualities", "radarr");

        exitCode.Should().Be(0);
    }

    [Test]
    public async Task Variable_overrides_cli_yml_key()
    {
        WriteKey("wrong-key");
        SetCliEnvironmentVariable(EnvVar, ApiKey);

        var exitCode = await RunCliAsync("list", "qualities", "radarr");

        exitCode.Should().Be(0);
    }

    [Test]
    public async Task Empty_variable_falls_back_to_cli_yml_key()
    {
        WriteKey(ApiKey);
        SetCliEnvironmentVariable(EnvVar, "");

        var exitCode = await RunCliAsync("list", "qualities", "radarr");

        exitCode.Should().Be(0);
    }

    [TestCase(null, TestName = "Missing key fails the command and names where to set it")]
    [TestCase("wrong-key", TestName = "Wrong key fails the command and names where to set it")]
    public async Task Rejected_key_names_where_to_set_it(string? key)
    {
        WriteKey(key);

        var exitCode = await RunCliAsync("list", "qualities", "radarr");

        exitCode.Should().Be(1);
        LogOutput.Should().Contain("cli.yml").And.Contain(EnvVar);
    }

    [Test]
    public async Task Rejected_key_on_sync_names_where_to_set_it()
    {
        WriteKey("wrong-key");
        AddInstanceConfig("movies");

        var exitCode = await RunCliAsync("sync");

        exitCode.Should().Be(1);
        LogOutput.Should().Contain("cli.yml").And.Contain(EnvVar);
    }

    [Test]
    public async Task Malformed_key_names_the_yaml_location()
    {
        WriteCliSettings("server: {base_url: http://localhost, api_key: [a]}");

        var exitCode = await RunCliAsync("list", "qualities", "radarr");

        exitCode.Should().Be(1);
        LogOutput.Should().Contain("cli.yml").And.Contain("'server.api_key'");
    }
}
