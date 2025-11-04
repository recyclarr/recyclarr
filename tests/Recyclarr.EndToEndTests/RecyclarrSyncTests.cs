using System.Reflection;
using AwesomeAssertions;
using CliWrap;
using CliWrap.Buffered;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Flurl;
using Flurl.Http;
using NUnit.Framework;

namespace Recyclarr.EndToEndTests;

internal sealed class RecyclarrSyncTests
{
    private static IContainer? _sonarrContainer;
    private static IContainer? _radarrContainer;
    private static string _sonarrUrl = string.Empty;
    private static string _radarrUrl = string.Empty;
    private static string _sonarrApiKey = string.Empty;
    private static string _radarrApiKey = string.Empty;
    private static string _recyclarrBinaryPath = string.Empty;
    private static string _publishPath = string.Empty;
    private static string _tempAppDataPath = string.Empty;

    private static string GetRepositoryRoot()
    {
        var assembly = typeof(RecyclarrSyncTests).Assembly;
        var attribute = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "RecyclarrRepositoryRoot");

        if (attribute?.Value is null)
        {
            throw new InvalidOperationException(
                "RecyclarrRepositoryRoot assembly metadata not found. Ensure the csproj defines this property."
            );
        }

        return Path.GetFullPath(attribute.Value);
    }

    [OneTimeSetUp]
    public static async Task OneTimeSetUp()
    {
        const string apiKey = "testkey";
        _sonarrApiKey = apiKey;
        _radarrApiKey = apiKey;

        var guid = Guid.NewGuid();
        _publishPath = Path.Combine(Path.GetTempPath(), $"recyclarr-e2e-publish-{guid}");
        _recyclarrBinaryPath = Path.Combine(_publishPath, "recyclarr");
        _tempAppDataPath = Path.Combine(Path.GetTempPath(), $"recyclarr-e2e-appdata-{guid}");
        Directory.CreateDirectory(_tempAppDataPath);

        var repositoryRoot = GetRepositoryRoot();
        var cliProjectPath = Path.Combine(repositoryRoot, "src", "Recyclarr.Cli");

        // Build container definitions
        _sonarrContainer = new ContainerBuilder()
            .WithImage("linuxserver/sonarr:latest")
            .WithPortBinding(8989, true)
            .WithEnvironment("SONARR__AUTH__APIKEY", apiKey)
            .WithTmpfsMount("/config")
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8989))
            )
            .WithCleanUp(true)
            .Build();

        _radarrContainer = new ContainerBuilder()
            .WithImage("linuxserver/radarr:latest")
            .WithPortBinding(7878, true)
            .WithEnvironment("RADARR__AUTH__APIKEY", apiKey)
            .WithTmpfsMount("/config")
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(7878))
            )
            .WithCleanUp(true)
            .Build();

        // Start all async operations in parallel
        var sonarrStartTask = _sonarrContainer.StartAsync();
        var radarrStartTask = _radarrContainer.StartAsync();
        var publishTask = Cli.Wrap("dotnet")
            .WithArguments(
                ["publish", cliProjectPath, "-c", "Release", "--self-contained", "-o", _publishPath]
            )
            .ExecuteAsync();

        await Task.WhenAll(sonarrStartTask, radarrStartTask, publishTask);

        _sonarrUrl = $"http://localhost:{_sonarrContainer.GetMappedPublicPort(8989)}";
        _radarrUrl = $"http://localhost:{_radarrContainer.GetMappedPublicPort(7878)}";
    }

    [OneTimeTearDown]
    public static async Task OneTimeTearDown()
    {
        if (_sonarrContainer is not null)
        {
            await _sonarrContainer.DisposeAsync();
        }

        if (_radarrContainer is not null)
        {
            await _radarrContainer.DisposeAsync();
        }

        if (Directory.Exists(_publishPath))
        {
            Directory.Delete(_publishPath, true);
        }

        if (Directory.Exists(_tempAppDataPath))
        {
            Directory.Delete(_tempAppDataPath, true);
        }
    }

    [Test]
    public async Task RecyclarrSync_WithComprehensiveConfig_SynchronizesAllFeatures()
    {
        var configPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Fixtures",
            "recyclarr.yml"
        );

        // Act - Execute recyclarr sync with environment variables
        var result = await Cli.Wrap(_recyclarrBinaryPath)
            .WithArguments(["sync", "--config", configPath])
            .WithEnvironmentVariables(env =>
                env.Set("SONARR_URL", _sonarrUrl)
                    .Set("SONARR_API_KEY", _sonarrApiKey)
                    .Set("RADARR_URL", _radarrUrl)
                    .Set("RADARR_API_KEY", _radarrApiKey)
                    .Set("RECYCLARR_APP_DATA", _tempAppDataPath)
            )
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        // Assert - Exit code should be 0
        await TestContext.Out.WriteLineAsync($"Recyclarr stdout:\n{result.StandardOutput}");
        await TestContext.Out.WriteLineAsync($"Recyclarr stderr:\n{result.StandardError}");

        result
            .ExitCode.Should()
            .Be(
                0,
                $"recyclarr sync failed with output:\n{result.StandardOutput}\n{result.StandardError}"
            );

        // Assert - Verify Sonarr quality profiles were synced
        var sonarrProfiles = await _sonarrUrl
            .AppendPathSegment("api/v3/qualityprofile")
            .WithHeader("X-Api-Key", _sonarrApiKey)
            .GetJsonAsync<List<QualityProfile>>();

        sonarrProfiles
            .Select(p => p.Name)
            .Should()
            .Contain("HD-1080p", "Sonarr should have synced HD-1080p quality profile");

        // Assert - Verify Sonarr custom formats were synced
        var sonarrCustomFormats = await _sonarrUrl
            .AppendPathSegment("api/v3/customformat")
            .WithHeader("X-Api-Key", _sonarrApiKey)
            .GetJsonAsync<List<CustomFormat>>();

        sonarrCustomFormats
            .Select(cf => cf.Name)
            .Should()
            .BeEquivalentTo(
                ["Bad Dual Groups", "No-RlsGroup", "Obfuscated"],
                "Sonarr should have exactly 3 synced custom formats"
            );

        // Assert - Verify Radarr quality profiles were synced
        var radarrProfiles = await _radarrUrl
            .AppendPathSegment("api/v3/qualityprofile")
            .WithHeader("X-Api-Key", _radarrApiKey)
            .GetJsonAsync<List<QualityProfile>>();

        radarrProfiles
            .Select(p => p.Name)
            .Should()
            .Contain("HD-1080p", "Radarr should have synced HD-1080p quality profile");

        // Assert - Verify Radarr custom formats were synced
        var radarrCustomFormats = await _radarrUrl
            .AppendPathSegment("api/v3/customformat")
            .WithHeader("X-Api-Key", _radarrApiKey)
            .GetJsonAsync<List<CustomFormat>>();

        radarrCustomFormats
            .Select(cf => cf.Name)
            .Should()
            .BeEquivalentTo(
                [
                    "Hybrid",
                    "Remaster",
                    "4K Remaster",
                    "Criterion Collection",
                    "Masters of Cinema",
                    "Vinegar Syndrome",
                    "Special Edition",
                ],
                "Radarr should have exactly 7 synced custom formats"
            );
    }

    [UsedImplicitly]
    private record QualityProfile(int Id, string Name);

    [UsedImplicitly]
    private record CustomFormat(int Id, string Name);
}
