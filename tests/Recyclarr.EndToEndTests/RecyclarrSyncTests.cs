using System.IO.Abstractions;
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

[TestFixture(Category = "E2E"), Explicit]
internal sealed class RecyclarrSyncTests
{
    private static readonly FileSystem FileSystem = new();
    private static IContainer? _sonarrContainer;
    private static IContainer? _radarrContainer;
    private static string _sonarrUrl = string.Empty;
    private static string _radarrUrl = string.Empty;
    private static string _sonarrApiKey = string.Empty;
    private static string _radarrApiKey = string.Empty;
    private static string _recyclarrBinaryPath = string.Empty;
    private static IDirectoryInfo _tempAppDataDir = null!;

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

    [OneTimeSetUp]
    public static async Task OneTimeSetUp()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        const string apiKey = "testkey";
        _sonarrApiKey = apiKey;
        _radarrApiKey = apiKey;

        var guid = Guid.NewGuid();
        var publishPath = FileSystem.DirectoryInfo.New(
            FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), $"recyclarr-e2e-publish-{guid}")
        );
        _recyclarrBinaryPath = publishPath.File("recyclarr").FullName;
        _tempAppDataDir = FileSystem.DirectoryInfo.New(
            FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), $"recyclarr-e2e-appdata-{guid}")
        );
        _tempAppDataDir.Create();

        // Copy settings.yml and resource provider fixtures to app data directory
        var fixturesDir = FileSystem.DirectoryInfo.New(
            FileSystem.Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures")
        );

        // Copy custom-formats-sonarr directory
        var sonarrCfsSource = fixturesDir.SubDirectory("custom-formats-sonarr");
        var sonarrCfsDest = _tempAppDataDir.SubDirectory("custom-formats-sonarr");
        if (sonarrCfsSource.Exists)
        {
            CopyDirectory(sonarrCfsSource, sonarrCfsDest);
        }

        // Copy custom-formats-radarr directory
        var radarrCfsSource = fixturesDir.SubDirectory("custom-formats-radarr");
        var radarrCfsDest = _tempAppDataDir.SubDirectory("custom-formats-radarr");
        if (radarrCfsSource.Exists)
        {
            CopyDirectory(radarrCfsSource, radarrCfsDest);
        }

        // Copy trash-guides-override directory
        var overrideSource = fixturesDir.SubDirectory("trash-guides-override");
        var overrideDest = _tempAppDataDir.SubDirectory("trash-guides-override");
        if (overrideSource.Exists)
        {
            CopyDirectory(overrideSource, overrideDest);
        }

        // Copy settings.yml and replace placeholders with absolute paths
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
            var settingsDest = _tempAppDataDir.File("settings.yml");
            await FileSystem.File.WriteAllTextAsync(settingsDest.FullName, settingsContent, ct);
        }

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
        var sonarrStartTask = _sonarrContainer.StartAsync(ct);
        var radarrStartTask = _radarrContainer.StartAsync(ct);
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

        if (_tempAppDataDir.Exists)
        {
            _tempAppDataDir.Delete(true);
        }
    }

    [Test]
    [CancelAfter(30_000)]
    public async Task RecyclarrSync_WithComprehensiveConfig_SynchronizesAllFeatures(
        CancellationToken ct
    )
    {
        var configPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Fixtures",
            "recyclarr.yml"
        );

        // Act - Execute recyclarr sync with environment variables
        var result = await Cli.Wrap(_recyclarrBinaryPath)
            .WithArguments(["sync", "--debug", "--config", configPath])
            .WithEnvironmentVariables(env =>
                env.Set("SONARR_URL", _sonarrUrl)
                    .Set("SONARR_API_KEY", _sonarrApiKey)
                    .Set("RADARR_URL", _radarrUrl)
                    .Set("RADARR_API_KEY", _radarrApiKey)
                    .Set("RECYCLARR_APP_DATA", _tempAppDataDir.FullName)
            )
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(ct);

        // Assert - Exit code should be 0
        await TestContext.Out.WriteLineAsync($"Recyclarr stdout:\n{result.StandardOutput}");
        await TestContext.Out.WriteLineAsync($"Recyclarr stderr:\n{result.StandardError}");

        result
            .ExitCode.Should()
            .Be(
                0,
                $"recyclarr sync failed with output:\n{result.StandardOutput}\n{result.StandardError}"
            );

        // Wait for async quality definition updates to complete (services return 202 Accepted)
        await WaitForQualityDefinitionUpdates(ct);

        // Assert - Verify Sonarr quality profiles were synced
        var sonarrProfiles = await _sonarrUrl
            .AppendPathSegment("api/v3/qualityprofile")
            .WithHeader("X-Api-Key", _sonarrApiKey)
            .GetJsonAsync<List<QualityProfile>>(cancellationToken: ct);

        sonarrProfiles
            .Select(p => p.Name)
            .Should()
            .Contain("HD-1080p", "Sonarr should have synced HD-1080p quality profile");

        sonarrProfiles
            .First(p => p.Name == "HD-1080p")
            .MinUpgradeFormatScore.Should()
            .Be(100, "Sonarr HD-1080p should have min_upgrade_format_score of 100");

        // Assert - Verify Sonarr custom formats were synced
        var sonarrCustomFormats = await _sonarrUrl
            .AppendPathSegment("api/v3/customformat")
            .WithHeader("X-Api-Key", _sonarrApiKey)
            .GetJsonAsync<List<CustomFormat>>(cancellationToken: ct);

        sonarrCustomFormats
            .Select(cf => cf.Name)
            .Should()
            .BeEquivalentTo(
                ["Bad Dual Groups", "No-RlsGroup", "Obfuscated", "E2E-SonarrCustom"],
                "Sonarr should have exactly 4 synced custom formats (3 official + 1 local)"
            );

        // Assert - Verify Radarr quality profiles were synced
        var radarrProfiles = await _radarrUrl
            .AppendPathSegment("api/v3/qualityprofile")
            .WithHeader("X-Api-Key", _radarrApiKey)
            .GetJsonAsync<List<QualityProfile>>(cancellationToken: ct);

        radarrProfiles
            .Select(p => p.Name)
            .Should()
            .Contain("HD-1080p", "Radarr should have synced HD-1080p quality profile");

        radarrProfiles
            .First(p => p.Name == "HD-1080p")
            .MinUpgradeFormatScore.Should()
            .Be(200, "Radarr HD-1080p should have min_upgrade_format_score of 200");

        // Assert - Verify Radarr custom formats were synced
        var radarrCustomFormats = await _radarrUrl
            .AppendPathSegment("api/v3/customformat")
            .WithHeader("X-Api-Key", _radarrApiKey)
            .GetJsonAsync<List<CustomFormat>>(cancellationToken: ct);

        radarrCustomFormats
            .Select(cf => cf.Name)
            .Should()
            .BeEquivalentTo(
                [
                    "Hybrid-OVERRIDE",
                    "Remaster",
                    "4K Remaster",
                    "Criterion Collection",
                    "Masters of Cinema",
                    "Vinegar Syndrome",
                    "Special Edition",
                    "E2E-TestFormat",
                ],
                "Radarr should have 8 CFs with 'Hybrid-OVERRIDE' proving last provider wins"
            );

        // Assert - Verify Sonarr quality definitions with explicit overrides
        var sonarrQualityDefs = await _sonarrUrl
            .AppendPathSegment("api/v3/qualitydefinition")
            .WithHeader("X-Api-Key", _sonarrApiKey)
            .GetJsonAsync<List<QualityDefinition>>(cancellationToken: ct);

        var sonarrHdtv1080P = sonarrQualityDefs.First(q => q.Title == "HDTV-1080p");
        sonarrHdtv1080P.MinSize.Should().Be(5, "Sonarr HDTV-1080p min should be overridden to 5");
        sonarrHdtv1080P.MaxSize.Should().Be(50, "Sonarr HDTV-1080p max should be overridden to 50");
        sonarrHdtv1080P
            .PreferredSize.Should()
            .Be(30, "Sonarr HDTV-1080p preferred should be overridden to 30");

        // Verify non-overridden quality keeps guide defaults
        var sonarrBluray1080P = sonarrQualityDefs.First(q => q.Title == "Bluray-1080p");
        sonarrBluray1080P
            .MinSize.Should()
            .Be(50.4m, "Sonarr Bluray-1080p min should be guide default");

        // Assert - Verify Radarr quality definitions with unlimited and explicit values
        var radarrQualityDefs = await _radarrUrl
            .AppendPathSegment("api/v3/qualitydefinition")
            .WithHeader("X-Api-Key", _radarrApiKey)
            .GetJsonAsync<List<QualityDefinition>>(cancellationToken: ct);

        var radarrBluray1080P = radarrQualityDefs.First(q => q.Title == "Bluray-1080p");
        radarrBluray1080P
            .MaxSize.Should()
            .BeNull("Radarr Bluray-1080p max should be null (unlimited)");
        radarrBluray1080P
            .PreferredSize.Should()
            .BeNull("Radarr Bluray-1080p preferred should be null (unlimited)");

        var radarrHdtv1080P = radarrQualityDefs.First(q => q.Title == "HDTV-1080p");
        radarrHdtv1080P.MinSize.Should().Be(0, "Radarr HDTV-1080p min should be 0");
        radarrHdtv1080P.MaxSize.Should().Be(100, "Radarr HDTV-1080p max should be 100");
        radarrHdtv1080P.PreferredSize.Should().Be(50, "Radarr HDTV-1080p preferred should be 50");

        // Verify non-overridden quality keeps guide defaults
        var radarrWebdl1080P = radarrQualityDefs.First(q => q.Title == "WEBDL-1080p");
        radarrWebdl1080P
            .MinSize.Should()
            .Be(12.5m, "Radarr WEBDL-1080p min should be guide default");
    }

    private static async Task WaitForQualityDefinitionUpdates(CancellationToken ct)
    {
        var timeout = TimeSpan.FromSeconds(10);
        var pollInterval = TimeSpan.FromMilliseconds(500);
        var deadline = DateTime.UtcNow + timeout;

        // Poll Sonarr - expect HDTV-1080p MinSize=5 (from quality override)
        while (DateTime.UtcNow < deadline)
        {
            var sonarrDefs = await _sonarrUrl
                .AppendPathSegment("api/v3/qualitydefinition")
                .WithHeader("X-Api-Key", _sonarrApiKey)
                .GetJsonAsync<List<QualityDefinition>>(cancellationToken: ct);

            var sonarrHdtv = sonarrDefs.FirstOrDefault(q => q.Title == "HDTV-1080p");
            if (sonarrHdtv?.MinSize == 5)
            {
                break;
            }

            await Task.Delay(pollInterval, ct);
        }

        // Poll Radarr - expect HDTV-1080p MinSize=0 (from quality override)
        while (DateTime.UtcNow < deadline)
        {
            var radarrDefs = await _radarrUrl
                .AppendPathSegment("api/v3/qualitydefinition")
                .WithHeader("X-Api-Key", _radarrApiKey)
                .GetJsonAsync<List<QualityDefinition>>(cancellationToken: ct);

            var radarrHdtv = radarrDefs.FirstOrDefault(q => q.Title == "HDTV-1080p");
            if (radarrHdtv?.MinSize == 0)
            {
                break;
            }

            await Task.Delay(pollInterval, ct);
        }
    }

    [UsedImplicitly]
    private record QualityProfile(int Id, string Name, int MinUpgradeFormatScore);

    [UsedImplicitly]
    private record CustomFormat(int Id, string Name);

    [UsedImplicitly]
    private record QualityDefinition(
        int Id,
        string Title,
        decimal MinSize,
        decimal? MaxSize,
        decimal? PreferredSize
    );
}
