using System.Diagnostics;
using System.Globalization;
using System.IO.Abstractions;
using System.Net;
using System.Net.Sockets;
using System.Text;

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
    private string? _apiKey = "test-server-api-key-0123456789abcdef";
    private string _apiUrl = "http://127.0.0.1:0";
    private string? _dataDirectory;
    private int _managementPort;
    private bool _managementPortFromEnvironment = true;
    private IDirectoryInfo ConfigDirectory => _root.SubDirectory("config");

    [SetUp]
    public void CreateRoot()
    {
        // A fixed management port would collide with parallel tests and a developer's server.
        _managementPort = FreePort();
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

    [Test]
    public async Task An_ephemeral_server_without_an_api_key_explains_why_on_stderr()
    {
        _apiKey = null;

        using var server = Start($"--parent-pid={Environment.ProcessId}", "--ephemeral=true");
        var output = server.StandardOutput.ReadToEndAsync();
        var errors = server.StandardError.ReadToEndAsync();
        await WaitForExitAsync(server);

        server.ExitCode.Should().NotBe(0);
        (await output).Should().NotContain("READY:");
        (await errors).Should().Contain("RECYCLARR_API_KEY");
    }

    /// <summary>
    /// The CLI launches an ephemeral server for each command, which would otherwise race a running
    /// server on the resource cache, state files, and log files.
    /// </summary>
    [Test]
    public async Task A_second_server_on_the_same_data_directory_stops_startup()
    {
        using var first = Start($"--parent-pid={Environment.ProcessId}");
        await WaitForReadyAsync(first);

        using var second = Start($"--parent-pid={Environment.ProcessId}", "--ephemeral=true");
        var output = second.StandardOutput.ReadToEndAsync();
        var errors = second.StandardError.ReadToEndAsync();
        await WaitForExitAsync(second);

        second.ExitCode.Should().Be(1);
        (await output).Should().NotContain("READY:");
        (await errors).Should().Contain("Another Recyclarr server").And.Contain("cli.yml");
        first.StandardInput.Close();
        await WaitForExitAsync(first);
    }

    [Test]
    public async Task A_second_server_sharing_only_the_state_directory_stops_startup()
    {
        using var first = Start($"--parent-pid={Environment.ProcessId}");
        await WaitForReadyAsync(first);

        _dataDirectory = _root.SubDirectory("other-data").FullName;
        using var second = Start($"--parent-pid={Environment.ProcessId}");
        var output = second.StandardOutput.ReadToEndAsync();
        var errors = second.StandardError.ReadToEndAsync();
        await WaitForExitAsync(second);

        second.ExitCode.Should().Be(1);
        (await output).Should().NotContain("READY:");
        (await errors).Should().Contain("Another Recyclarr server").And.Contain("state");
        first.StandardInput.Close();
        await WaitForExitAsync(first);
    }

    [Test]
    public async Task A_server_that_crashed_does_not_block_the_next_start()
    {
        using (var crashed = Start($"--parent-pid={Environment.ProcessId}"))
        {
            await WaitForReadyAsync(crashed);
            crashed.Kill(entireProcessTree: true);
            await WaitForExitAsync(crashed);
        }

        using var next = Start($"--parent-pid={Environment.ProcessId}");
        await WaitForReadyAsync(next);
        next.StandardInput.Close();
        await WaitForExitAsync(next);

        next.ExitCode.Should().Be(0);
    }

    /// <summary>
    /// Probes and metrics are anonymous, so they are served only on the management port, and the
    /// API only on its own port (ADR-029).
    /// </summary>
    [TestCase("/livez", true)]
    [TestCase("/readyz", true)]
    [TestCase("/metrics", true)]
    [TestCase("/openapi/v1.json", false)]
    public async Task Each_route_is_served_only_on_its_own_port(string path, bool management)
    {
        using var server = Start($"--parent-pid={Environment.ProcessId}");
        var apiPort = await WaitForReadyAsync(server);
        using var http = new HttpClient();

        var onManagement = await http.GetAsync(LocalUri(_managementPort, path));
        var onApi = await http.GetAsync(LocalUri(apiPort, path));

        var (servedBy, refusedBy) = management ? (onManagement, onApi) : (onApi, onManagement);
        servedBy.StatusCode.Should().Be(HttpStatusCode.OK);
        refusedBy.StatusCode.Should().Be(HttpStatusCode.NotFound);
        server.StandardInput.Close();
        await WaitForExitAsync(server);
    }

    [Test]
    public async Task Management_settings_choose_the_port_and_can_turn_metrics_off()
    {
        _managementPortFromEnvironment = false;
        ConfigDirectory
            .File("settings.yml")
            .AppendAllText(
                $"\nserver:\n  management:\n    port: {_managementPort}\n    metrics: false\n"
            );

        using var server = Start($"--parent-pid={Environment.ProcessId}");
        await WaitForReadyAsync(server);
        using var http = new HttpClient();

        var probe = await http.GetAsync(LocalUri(_managementPort, "/livez"));
        var metrics = await http.GetAsync(LocalUri(_managementPort, "/metrics"));

        probe.StatusCode.Should().Be(HttpStatusCode.OK);
        metrics.StatusCode.Should().Be(HttpStatusCode.NotFound);
        server.StandardInput.Close();
        await WaitForExitAsync(server);
    }

    [Test]
    public async Task An_ephemeral_server_opens_no_management_port()
    {
        using var server = Start($"--parent-pid={Environment.ProcessId}", "--ephemeral=true");
        await WaitForReadyAsync(server);
        using var http = new HttpClient();

        var probe = () => http.GetAsync(LocalUri(_managementPort, "/livez"));

        await probe.Should().ThrowAsync<HttpRequestException>();
        server.StandardInput.Close();
        await WaitForExitAsync(server);
    }

    [Test]
    public async Task A_management_port_equal_to_the_api_port_stops_startup()
    {
        _apiUrl = $"http://127.0.0.1:{_managementPort}";

        using var server = Start($"--parent-pid={Environment.ProcessId}");
        var output = server.StandardOutput.ReadToEndAsync();
        var errors = server.StandardError.ReadToEndAsync();
        await WaitForExitAsync(server);

        // Startup fails before settings configure logging, so the reason is on stderr.
        server.ExitCode.Should().NotBe(0);
        (await output).Should().NotContain("READY:");
        (await errors).Should().Contain("management port");
    }

    [TestCase(0)]
    [TestCase(70000)]
    public async Task An_out_of_range_management_port_stops_startup(int port)
    {
        _managementPort = port;

        using var server = Start($"--parent-pid={Environment.ProcessId}");
        var output = server.StandardOutput.ReadToEndAsync();
        var errors = server.StandardError.ReadToEndAsync();
        await WaitForExitAsync(server);

        server.ExitCode.Should().Be(1);
        (await output).Should().NotContain("READY:");
        (await errors).Should().Contain("between 1 and 65535");
    }

    /// <summary>
    /// Log correlation needs a current activity, which exists only while a tracer listens, so the
    /// server must trace even when no exporter is configured.
    /// </summary>
    [Test]
    public async Task Sync_job_log_lines_carry_a_trace_id_without_an_exporter()
    {
        ConfigDirectory
            .File("recyclarr.yml")
            .WriteAllText(
                """
                radarr:
                  movies:
                    base_url: http://127.0.0.1:1
                    api_key: abc
                """
            );

        using var server = Start($"--parent-pid={Environment.ProcessId}");
        var apiPort = await WaitForReadyAsync(server);
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new("Bearer", _apiKey);

        using var body = new StringContent("{}", Encoding.UTF8, "application/json");
        using var created = await http.PostAsync(LocalUri(apiPort, "/api/v1/sync/jobs"), body);
        created.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var jobLine = await ReadLineAsync(
            server,
            line => line.Contains("\"Sync job ", StringComparison.Ordinal)
        );
        jobLine.Should().Contain("\"trace_id\":");
        server.StandardInput.Close();
        await WaitForExitAsync(server);
    }

    private static Uri LocalUri(int port, string path) => new($"http://127.0.0.1:{port}{path}");

    private static async Task<string> ReadLineAsync(Process server, Func<string, bool> match)
    {
        using var timeout = new CancellationTokenSource(ExitTimeout);
        while (await server.StandardOutput.ReadLineAsync(timeout.Token) is { } line)
        {
            if (match(line))
            {
                return line;
            }
        }

        Assert.Fail("The server exited before writing the expected line");
        return "";
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
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
            Environment =
            {
                ["RECYCLARR_CONFIG_DIR"] = ConfigDirectory.FullName,
                ["RECYCLARR_API_KEY"] = _apiKey,
                ["RECYCLARR_DATA_DIR"] = _dataDirectory,
            },
        };

        if (_managementPortFromEnvironment)
        {
            startInfo.Environment["ASPNETCORE_MANAGEMENTPORT"] = _managementPort.ToString(
                CultureInfo.InvariantCulture
            );
        }

        startInfo.ArgumentList.Add($"--urls={_apiUrl}");
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // non-null: Start returns null only when reusing a shell-executed process
        return Process.Start(startInfo)!;
    }

    /// <summary>
    /// A persistent server also logs to stdout, so lines before READY are skipped. Returns the API
    /// port the server reports.
    /// </summary>
    private static async Task<int> WaitForReadyAsync(Process server)
    {
        const string prefix = "READY:";
        using var timeout = new CancellationTokenSource(ExitTimeout);
        while (await server.StandardOutput.ReadLineAsync(timeout.Token) is { } line)
        {
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                return int.Parse(line[prefix.Length..], CultureInfo.InvariantCulture);
            }
        }

        Assert.Fail("The server exited before it was ready");
        return -1;
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
