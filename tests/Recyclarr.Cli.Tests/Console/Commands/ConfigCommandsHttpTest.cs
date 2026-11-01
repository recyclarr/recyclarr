using System.IO.Abstractions;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Platform;

namespace Recyclarr.Cli.Tests.Console.Commands;

/// <summary>
/// Runs <c>config create</c> and <c>config list templates</c> against the in-process server. The
/// server generates templates from guide data; the CLI writes them into its own configuration
/// directory.
/// </summary>
internal sealed class ConfigCommandsHttpTest : CliServerHttpFixture
{
    private IDirectoryInfo CliConfigDirectory => ResolveCli<ConfigDirectoryLocator>().Locate();

    private IFileSystem CliFs => ResolveCli<IFileSystem>();

    private void AddGuide()
    {
        var root = Fs.CurrentDirectory().SubDirectory("guide");
        Fs.AddFile(
            root.File("metadata.json"),
            new MockFileData(
                """
                {
                  "json_paths": {
                    "radarr": {
                      "custom_formats": [], "qualities": [], "naming": [],
                      "quality_profiles": ["radarr-qp"], "custom_format_groups": []
                    },
                    "sonarr": {
                      "custom_formats": [], "qualities": [], "naming": [],
                      "quality_profiles": ["sonarr-qp"], "custom_format_groups": []
                    }
                  }
                }
                """
            )
        );
        Fs.AddFile(
            root.SubDirectory("radarr-qp").File("hd-bluray-web.json"),
            new MockFileData(
                """{ "trash_id": "qp-hd", "name": "HD Bluray + WEB", "items": [], "formatItems": {} }"""
            )
        );
        Fs.AddFile(
            root.SubDirectory("sonarr-qp").File("web-1080p.json"),
            new MockFileData(
                """{ "trash_id": "qp-web", "name": "WEB-1080p", "items": [], "formatItems": {} }"""
            )
        );

        var includes = Fs.CurrentDirectory().SubDirectory("includes");
        Fs.AddFile(
            includes.File("includes.json"),
            new MockFileData("""{ "radarr": [{ "id": "radarr-inc", "template": "r.yml" }] }""")
        );

        Fs.AddFile(
            Paths.ConfigDirectory.File("settings.yml"),
            new MockFileData(
                $"""
                resource_providers:
                  - name: local-guide
                    type: trash-guides
                    path: {root.FullName}
                  - name: my-includes
                    type: config-templates
                    path: {includes.FullName}
                """
            )
        );
    }

    private string ReadCliFile(IFileInfo file)
    {
        return ReadCliFile(file.FullName);
    }

    private string ReadCliFile(string path)
    {
        return CliFs.File.ReadAllText(path);
    }

    [Test]
    public async Task Create_without_template_writes_the_starter_config_to_the_config_directory()
    {
        var exitCode = await RunCliAsync("config", "create");

        exitCode.Should().Be(0);
        ReadCliFile(CliConfigDirectory.File("recyclarr.yml")).Should().Contain("api_key:");
    }

    [Test]
    public async Task Create_with_path_writes_the_starter_config_there()
    {
        var path = CliFs.CurrentDirectory().SubDirectory("elsewhere").File("mine.yml");

        var exitCode = await RunCliAsync("config", "create", "-p", path.FullName);

        exitCode.Should().Be(0);
        ReadCliFile(path).Should().Contain("api_key:");
    }

    [Test]
    public async Task Create_refuses_to_replace_an_existing_file_without_force()
    {
        var path = AddCliFile("existing.yml", "keep me");

        var exitCode = await RunCliAsync("config", "create", "-p", path);

        exitCode.Should().Be(1);
        ReadCliFile(path).Should().Be("keep me");
        LogOutput.Should().Contain("File already exists").And.Contain("--force");
    }

    [Test]
    public async Task Create_with_force_replaces_an_existing_file()
    {
        var path = AddCliFile("existing.yml", "replace me");

        var exitCode = await RunCliAsync("config", "create", "-p", path, "--force");

        exitCode.Should().Be(0);
        ReadCliFile(path).Should().Contain("api_key:");
        LogOutput.Should().Contain("Replaced");
    }

    [Test]
    public async Task Create_with_templates_writes_each_into_the_configs_directory()
    {
        AddGuide();

        var exitCode = await RunCliAsync(
            "config",
            "create",
            "-t",
            "hd-bluray-web",
            "-t",
            "web-1080p"
        );

        exitCode.Should().Be(0);
        var configs = CliConfigDirectory.SubDirectory("configs");
        ReadCliFile(configs.File("hd-bluray-web.yml")).Should().Contain("trash_id: qp-hd");
        ReadCliFile(configs.File("web-1080p.yml")).Should().Contain("trash_id: qp-web");
    }

    [Test]
    public async Task Create_with_an_unknown_template_fails_but_writes_the_known_ones()
    {
        AddGuide();

        var exitCode = await RunCliAsync("config", "create", "-t", "nope", "-t", "web-1080p");

        exitCode.Should().Be(1);
        LogOutput.Should().Contain("No template has the id 'nope'");
        var configs = CliConfigDirectory.SubDirectory("configs");
        configs.File("nope.yml").Exists.Should().BeFalse();
        configs.File("web-1080p.yml").Exists.Should().BeTrue();
    }

    [Test]
    public async Task Create_with_one_template_and_path_writes_the_template_there()
    {
        AddGuide();
        var path = CliFs.CurrentDirectory().File("mine.yml");

        var exitCode = await RunCliAsync(
            "config",
            "create",
            "-t",
            "web-1080p",
            "-p",
            path.FullName
        );

        exitCode.Should().Be(0);
        ReadCliFile(path).Should().Contain("trash_id: qp-web");
    }

    [Test]
    public async Task Create_with_path_and_several_templates_is_rejected()
    {
        AddGuide();
        var path = CliFs.CurrentDirectory().File("mine.yml");

        var exitCode = await RunCliAsync(
            "config",
            "create",
            "-t",
            "hd-bluray-web",
            "-t",
            "web-1080p",
            "-p",
            path.FullName
        );

        exitCode.Should().NotBe(0);
        LogOutput.Should().Contain("Cannot use --path with more than one --template");
        path.Refresh();
        path.Exists.Should().BeFalse();
    }

    [TestCase("radarr\thd-bluray-web\tHD Bluray + WEB\n", "radarr-inc")]
    [TestCase("radarr\tradarr-inc\n", "hd-bluray-web", "-i")]
    public async Task List_templates_raw_prints_service_and_id(
        string expected,
        string unexpected,
        params string[] extraArgs
    )
    {
        AddGuide();

        var exitCode = await RunCliAsync(["config", "list", "templates", "--raw", .. extraArgs]);

        exitCode.Should().Be(0);
        ConsoleOutput.Should().Contain(expected).And.NotContain(unexpected);
    }
}
