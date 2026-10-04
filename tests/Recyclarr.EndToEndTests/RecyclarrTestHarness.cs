using System.IO.Abstractions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using CliWrap;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using NUnit.Framework;
using Recyclarr.Client.V1;
using Refit;

namespace Recyclarr.EndToEndTests;

internal sealed class RecyclarrTestHarness : IAsyncDisposable
{
    private static readonly FileSystem FileSystem = new();

    // Match production serialization: null properties must be omitted from request bodies
    private static readonly RefitSettings ServarrRefitSettings = new()
    {
        ContentSerializer = new SystemTextJsonContentSerializer(
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
            }
        ),
    };

    // Match the CLI's self-API settings: enums are camelCase strings and unset request fields are
    // omitted, because the server rejects explicit nulls for non-nullable fields. Responses get
    // the same null rule.
    private static readonly RefitSettings ServerRefitSettings = new()
    {
        ContentSerializer = new SystemTextJsonContentSerializer(
            new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                RespectNullableAnnotations = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            }
        ),
    };

    private const string ApiKey = "testkey";

    private readonly HttpClient _sonarr;
    private readonly HttpClient _radarr;
    private readonly IContainer _sonarrContainer;
    private readonly IContainer _radarrContainer;
    private readonly string _serverBinaryPath;
    private readonly IDirectoryInfo _fixturesDir;
    private RecyclarrServerProcess? _server;
    private HttpClient? _serverClient;
    private ISyncApi? _sync;

    /// <summary>
    /// The server's config directory. It also holds sync state, which must survive server
    /// restarts so later syncs recognize resources that earlier syncs created.
    /// </summary>
    public IDirectoryInfo AppDataDir { get; }

    public ISyncApi Sync =>
        _sync ?? throw new InvalidOperationException("The server has not been started.");

    public T SonarrApi<T>()
        where T : class => RestService.For<T>(_sonarr, ServarrRefitSettings);

    public T RadarrApi<T>()
        where T : class => RestService.For<T>(_radarr, ServarrRefitSettings);

    private RecyclarrTestHarness(
        HttpClient sonarr,
        HttpClient radarr,
        IContainer sonarrContainer,
        IContainer radarrContainer,
        string serverBinaryPath,
        IDirectoryInfo fixturesDir,
        IDirectoryInfo appDataDir
    )
    {
        _sonarr = sonarr;
        _radarr = radarr;
        _sonarrContainer = sonarrContainer;
        _radarrContainer = radarrContainer;
        _serverBinaryPath = serverBinaryPath;
        _fixturesDir = fixturesDir;
        AppDataDir = appDataDir;
    }

    /// <summary>
    /// Starts Sonarr and Radarr containers and publishes the server. Call
    /// <see cref="StartServerAsync"/> before syncing.
    /// </summary>
    public static async Task<RecyclarrTestHarness> StartAsync(TestContext testContext)
    {
        var ct = testContext.CancellationToken;

        var guid = Guid.NewGuid();
        var publishPath = FileSystem.DirectoryInfo.New(
            FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), $"recyclarr-e2e-publish-{guid}")
        );
        var serverBinaryPath = publishPath.File("recyclarr-server").FullName;
        var appDataDir = FileSystem.DirectoryInfo.New(
            FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), $"recyclarr-e2e-appdata-{guid}")
        );
        appDataDir.Create();

        var fixturesDir = FileSystem.DirectoryInfo.New(
            FileSystem.Path.Combine(testContext.TestDirectory, "Fixtures")
        );
        await SetUpFixtures(appDataDir, fixturesDir, ct);

        var repositoryRoot = GetRepositoryRoot();
        var serverProjectPath = Path.Combine(repositoryRoot, "src", "Recyclarr.Server");

        var sonarrContainer = new ContainerBuilder("linuxserver/sonarr:latest")
            .WithPortBinding(8989, true)
            .WithEnvironment("SONARR__AUTH__APIKEY", ApiKey)
            .WithTmpfsMount("/config")
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8989))
            )
            .WithCleanUp(true)
            .Build();

        var radarrContainer = new ContainerBuilder("linuxserver/radarr:latest")
            .WithPortBinding(7878, true)
            .WithEnvironment("RADARR__AUTH__APIKEY", ApiKey)
            .WithTmpfsMount("/config")
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(7878))
            )
            .WithCleanUp(true)
            .Build();

        var sonarrStartTask = sonarrContainer.StartAsync(ct);
        var radarrStartTask = radarrContainer.StartAsync(ct);
        var publishTask = Cli.Wrap("dotnet")
            .WithArguments([
                "publish",
                serverProjectPath,
                "-c",
                "Release",
                "-o",
                publishPath.FullName,
            ])
            .ExecuteAsync(ct);

        await Task.WhenAll(sonarrStartTask, radarrStartTask, publishTask);

        var sonarrUrl = $"http://localhost:{sonarrContainer.GetMappedPublicPort(8989)}";
        var radarrUrl = $"http://localhost:{radarrContainer.GetMappedPublicPort(7878)}";

        var sonarr = CreateHttpClient(sonarrUrl, ApiKey);
        var radarr = CreateHttpClient(radarrUrl, ApiKey);

        return new RecyclarrTestHarness(
            sonarr,
            radarr,
            sonarrContainer,
            radarrContainer,
            serverBinaryPath,
            fixturesDir,
            appDataDir
        );
    }

    /// <summary>
    /// Installs the named fixture as the server's only configuration file and (re)starts the
    /// server so it loads that configuration.
    /// </summary>
    public async Task StartServerAsync(string configFixtureName, CancellationToken ct)
    {
        await StopServerAsync();

        _fixturesDir
            .File(configFixtureName)
            .CopyTo(AppDataDir.File("recyclarr.yml").FullName, overwrite: true);

        var sonarrUrl = $"http://localhost:{_sonarrContainer.GetMappedPublicPort(8989)}";
        var radarrUrl = $"http://localhost:{_radarrContainer.GetMappedPublicPort(7878)}";

        _server = await RecyclarrServerProcess.StartAsync(
            _serverBinaryPath,
            new Dictionary<string, string?>
            {
                ["SONARR_URL"] = sonarrUrl,
                ["SONARR_API_KEY"] = ApiKey,
                ["RADARR_URL"] = radarrUrl,
                ["RADARR_API_KEY"] = ApiKey,
                // Pin both directories so a developer's own settings never leak into the run.
                ["RECYCLARR_CONFIG_DIR"] = AppDataDir.FullName,
                ["RECYCLARR_DATA_DIR"] = AppDataDir.FullName,
                ["RECYCLARR_APP_DATA"] = "",
            },
            ct
        );

        _serverClient = new HttpClient { BaseAddress = _server.BaseAddress };
        _sync = RestService.For<ISyncApi>(_serverClient, ServerRefitSettings);
    }

    private async Task StopServerAsync()
    {
        _sync = null;
        _serverClient?.Dispose();
        _serverClient = null;

        if (_server is not null)
        {
            await _server.DisposeAsync();
            _server = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopServerAsync();
        _sonarr.Dispose();
        _radarr.Dispose();
        await _sonarrContainer.DisposeAsync();
        await _radarrContainer.DisposeAsync();

        if (AppDataDir.Exists)
        {
            AppDataDir.Delete(true);
        }
    }

    private static HttpClient CreateHttpClient(string baseUrl, string apiKey)
    {
        var client = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        return client;
    }

    private static async Task SetUpFixtures(
        IDirectoryInfo appDataDir,
        IDirectoryInfo fixturesDir,
        CancellationToken ct
    )
    {
        var sonarrCfsSource = fixturesDir.SubDirectory("custom-formats-sonarr");
        var sonarrCfsDest = appDataDir.SubDirectory("custom-formats-sonarr");
        if (sonarrCfsSource.Exists)
        {
            CopyDirectory(sonarrCfsSource, sonarrCfsDest);
        }

        var radarrCfsSource = fixturesDir.SubDirectory("custom-formats-radarr");
        var radarrCfsDest = appDataDir.SubDirectory("custom-formats-radarr");
        if (radarrCfsSource.Exists)
        {
            CopyDirectory(radarrCfsSource, radarrCfsDest);
        }

        var overrideSource = fixturesDir.SubDirectory("trash-guides-override");
        var overrideDest = appDataDir.SubDirectory("trash-guides-override");
        if (overrideSource.Exists)
        {
            CopyDirectory(overrideSource, overrideDest);
        }

        var settingsSource = fixturesDir.File("settings.yml");
        if (settingsSource.Exists)
        {
            var settingsContent = await FileSystem.File.ReadAllTextAsync(
                settingsSource.FullName,
                ct
            );
            settingsContent = settingsContent
                .Replace(
                    "PLACEHOLDER_SONARR_CFS_PATH",
                    sonarrCfsDest.FullName,
                    StringComparison.Ordinal
                )
                .Replace(
                    "PLACEHOLDER_RADARR_CFS_PATH",
                    radarrCfsDest.FullName,
                    StringComparison.Ordinal
                )
                .Replace(
                    "PLACEHOLDER_OVERRIDE_PATH",
                    overrideDest.FullName,
                    StringComparison.Ordinal
                );
            var settingsDest = appDataDir.File("settings.yml");
            await FileSystem.File.WriteAllTextAsync(settingsDest.FullName, settingsContent, ct);
        }
    }

    private static string GetRepositoryRoot()
    {
        var assembly = typeof(RecyclarrTestHarness).Assembly;
        var attribute = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "RecyclarrRepositoryRoot");

        if (attribute?.Value is null)
        {
            throw new InvalidOperationException(
                "RecyclarrRepositoryRoot assembly metadata not found."
            );
        }

        return Path.GetFullPath(attribute.Value);
    }

    private static void CopyDirectory(IDirectoryInfo sourceDir, IDirectoryInfo destDir)
    {
        destDir.Create();

        foreach (var file in sourceDir.GetFiles())
        {
            var destFile = destDir.File(file.Name);
            file.CopyTo(destFile.FullName);
        }

        foreach (var dir in sourceDir.GetDirectories())
        {
            var destSubDir = destDir.SubDirectory(dir.Name);
            CopyDirectory(dir, destSubDir);
        }
    }
}
