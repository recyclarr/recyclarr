using System.Diagnostics;
using System.Globalization;
using System.IO.Abstractions;

namespace Recyclarr.Server.Tests;

/// <summary>
/// Runs the built <c>recyclarr-server</c> binary as a real child process, because its launch
/// contract (command-line flags, stdin lifeline, stdout and stderr) only exists at the process
/// boundary. Local, empty resource providers replace the defaults so startup never clones.
/// </summary>
internal sealed class ServerProcessTest
{
    private static readonly TimeSpan ExitTimeout = TimeSpan.FromSeconds(30);

    private IDirectoryInfo _root = null!;
    private IDirectoryInfo ConfigDirectory => _root.SubDirectory("config");

    [SetUp]
    public void CreateRoot()
    {
        _root = new FileSystem().Directory.CreateTempSubdirectory("recyclarr-server-process-");
        var guides = _root.SubDirectory("guides");
        var templates = _root.SubDirectory("templates");
        guides.Create();
        templates.Create();
        ConfigDirectory.Create();

        guides
            .File("metadata.json")
            .WriteAllText(
                """
                {
                  "json_paths": {
                    "radarr": { "custom_formats": [], "qualities": [], "naming": [],
                      "custom_format_groups": [], "quality_profiles": [] },
                    "sonarr": { "custom_formats": [], "qualities": [], "naming": [],
                      "custom_format_groups": [], "quality_profiles": [] }
                  }
                }
                """
            );

        ConfigDirectory
            .File("settings.yml")
            .WriteAllText(
                $"""
                resource_providers:
                  - name: guides
                    type: trash-guides
                    path: {guides.FullName}
                    replace_default: true
                  - name: templates
                    type: config-templates
                    path: {templates.FullName}
                    replace_default: true
                """
            );
    }

    [TearDown]
    public void DeleteRoot()
    {
        _root.Delete(recursive: true);
    }

    /// <summary>
    /// `recyclarr serve` launches a persistent server that must still stop with the CLI.
    /// </summary>
    [Test]
    public async Task A_persistent_server_with_a_parent_stops_when_its_lifeline_closes()
    {
        using var server = Start($"--parent-pid={Environment.ProcessId}");
        await WaitForReadyAsync(server);

        server.StandardInput.Close();
        await WaitForExitAsync(server);

        server.ExitCode.Should().Be(0);
        StateDatabase.Exists.Should().BeTrue();
    }

    /// <summary>
    /// Ctrl+C reaches the server directly while its lifeline is still open (the CLI outlives the
    /// server to report its exit code). The server must not wait out the host shutdown timeout.
    /// </summary>
    [Test]
    [Platform(Exclude = "Win", Reason = "Sends SIGINT with the kill command")]
    public async Task A_server_with_an_open_lifeline_stops_promptly_on_interrupt()
    {
        using var server = Start($"--parent-pid={Environment.ProcessId}");
        await WaitForReadyAsync(server);

        using (
            var kill = Process.Start(
                "kill",
                ["-INT", server.Id.ToString(CultureInfo.InvariantCulture)]
            )
        )
        {
            await kill.WaitForExitAsync();
        }

        await WaitForExitAsync(server, TimeSpan.FromSeconds(10));
        server.ExitCode.Should().Be(0);
    }

    [Test]
    public async Task An_ephemeral_server_keeps_no_database_and_writes_only_readiness_to_stdout()
    {
        using var server = Start($"--parent-pid={Environment.ProcessId}", "--ephemeral=true");
        var errors = server.StandardError.ReadToEndAsync();
        await WaitForReadyAsync(server);

        server.StandardInput.Close();
        var remainingOutput = await server.StandardOutput.ReadToEndAsync();
        await WaitForExitAsync(server);

        remainingOutput.Should().BeEmpty();
        (await errors).Should().BeEmpty();
        StateDatabase.Exists.Should().BeFalse();
    }

    [Test]
    public async Task An_ephemeral_server_that_cannot_start_explains_why_on_stderr()
    {
        ConfigDirectory
            .File("recyclarr.yml")
            .WriteAllText(
                """
                radarr:
                  movies:
                    base_url: not a url
                    api_key: abc
                """
            );

        using var server = Start($"--parent-pid={Environment.ProcessId}", "--ephemeral=true");
        var output = server.StandardOutput.ReadToEndAsync();
        var errors = server.StandardError.ReadToEndAsync();
        await WaitForExitAsync(server);

        server.ExitCode.Should().NotBe(0);
        (await output).Should().NotContain("READY:");
        (await errors).Should().Contain("base_url must start with 'http' or 'https'");
    }

    private IFileInfo StateDatabase => ConfigDirectory.SubDirectory("state").File("server.db");

    private Process Start(params string[] arguments)
    {
        var binaryName = OperatingSystem.IsWindows() ? "recyclarr-server.exe" : "recyclarr-server";
        var binary = _root.FileSystem.DirectoryInfo.New(AppContext.BaseDirectory).File(binaryName);
        var startInfo = new ProcessStartInfo(binary.FullName)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Environment = { ["RECYCLARR_CONFIG_DIR"] = ConfigDirectory.FullName },
        };

        startInfo.ArgumentList.Add("--urls=http://127.0.0.1:0");
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // non-null: Start returns null only when reusing a shell-executed process
        return Process.Start(startInfo)!;
    }

    /// <summary>
    /// A persistent server also logs to stdout, so lines before READY are skipped.
    /// </summary>
    private static async Task WaitForReadyAsync(Process server)
    {
        using var timeout = new CancellationTokenSource(ExitTimeout);
        while (await server.StandardOutput.ReadLineAsync(timeout.Token) is { } line)
        {
            if (line.StartsWith("READY:", StringComparison.Ordinal))
            {
                return;
            }
        }

        Assert.Fail("The server exited before it was ready");
    }

    private static async Task WaitForExitAsync(Process server, TimeSpan? limit = null)
    {
        using var timeout = new CancellationTokenSource(limit ?? ExitTimeout);
        try
        {
            await server.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            server.Kill(entireProcessTree: true);
            Assert.Fail("The server did not exit");
        }
    }
}
