using System.IO.Abstractions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using CliWrap;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using NUnit.Framework;
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

    private readonly HttpClient _sonarr;
    private readonly HttpClient _radarr;
    private readonly IContainer _sonarrContainer;
    private readonly IContainer _radarrContainer;

    public string RecyclarrBinaryPath { get; }
    public IDirectoryInfo AppDataDir { get; }
    public string ConfigPath { get; }
    public string ConfigPathDeleteDisabled { get; }

    public T SonarrApi<T>()
        where T : class => RestService.For<T>(_sonarr, ServarrRefitSettings);

    public T RadarrApi<T>()
        where T : class => RestService.For<T>(_radarr, ServarrRefitSettings);

    // Expose ports for environment variable setup in RunRecyclarrSync
    public int SonarrPort => _sonarrContainer.GetMappedPublicPort(8989);
    public int RadarrPort => _radarrContainer.GetMappedPublicPort(7878);

    private RecyclarrTestHarness(
        HttpClient sonarr,
        HttpClient radarr,
        IContainer sonarrContainer,
        IContainer radarrContainer,
        string recyclarrBinaryPath,
        IDirectoryInfo appDataDir,
        string configPath,
        string configPathDeleteDisabled
    )
    {
        _sonarr = sonarr;
        _radarr = radarr;
        _sonarrContainer = sonarrContainer;
        _radarrContainer = radarrContainer;
        RecyclarrBinaryPath = recyclarrBinaryPath;
        AppDataDir = appDataDir;
        ConfigPath = configPath;
        ConfigPathDeleteDisabled = configPathDeleteDisabled;
    }

    public static async Task<RecyclarrTestHarness> StartAsync(TestContext testContext)
    {
        var ct = testContext.CancellationToken;
        const string apiKey = "testkey";

        var guid = Guid.NewGuid();
        var publishPath = FileSystem.DirectoryInfo.New(
            FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), $"recyclarr-e2e-publish-{guid}")
        );
        var recyclarrBinaryPath = publishPath.File("recyclarr").FullName;
        var appDataDir = FileSystem.DirectoryInfo.New(
            FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), $"recyclarr-e2e-appdata-{guid}")
        );
        appDataDir.Create();

        var configPath = Path.Combine(testContext.TestDirectory, "Fixtures", "recyclarr.yml");
        var configPathDeleteDisabled = Path.Combine(
            testContext.TestDirectory,
            "Fixtures",
            "recyclarr-delete-disabled.yml"
        );
        await SetUpFixtures(appDataDir, testContext.TestDirectory, ct);

        var repositoryRoot = GetRepositoryRoot();
        var cliProjectPath = Path.Combine(repositoryRoot, "src", "Recyclarr.Cli");

        var sonarrContainer = new ContainerBuilder("linuxserver/sonarr:latest")
            .WithPortBinding(8989, true)
            .WithEnvironment("SONARR__AUTH__APIKEY", apiKey)
            .WithTmpfsMount("/config")
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8989))
            )
            .WithCleanUp(true)
            .Build();

        var radarrContainer = new ContainerBuilder("linuxserver/radarr:latest")
            .WithPortBinding(7878, true)
            .WithEnvironment("RADARR__AUTH__APIKEY", apiKey)
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
                cliProjectPath,
                "-c",
                "Release",
                "--self-contained",
                "-o",
                publishPath.FullName,
            ])
            .ExecuteAsync(ct);

        await Task.WhenAll(sonarrStartTask, radarrStartTask, publishTask);

        var sonarrUrl = $"http://localhost:{sonarrContainer.GetMappedPublicPort(8989)}";
        var radarrUrl = $"http://localhost:{radarrContainer.GetMappedPublicPort(7878)}";

        var sonarr = CreateHttpClient(sonarrUrl, apiKey);
        var radarr = CreateHttpClient(radarrUrl, apiKey);

        return new RecyclarrTestHarness(
            sonarr,
            radarr,
            sonarrContainer,
            radarrContainer,
            recyclarrBinaryPath,
            appDataDir,
            configPath,
            configPathDeleteDisabled
        );
    }

    public async ValueTask DisposeAsync()
    {
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
        string testDirectory,
        CancellationToken ct
    )
    {
        var fixturesDir = FileSystem.DirectoryInfo.New(
            FileSystem.Path.Combine(testDirectory, "Fixtures")
        );

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
