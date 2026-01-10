using System.IO.Abstractions;
using System.Reflection;
using AwesomeAssertions;
using CliWrap;
using CliWrap.Buffered;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using NUnit.Framework;
using Recyclarr.EndToEndTests.Clients;

namespace Recyclarr.EndToEndTests;

[TestFixture(Category = "E2E"), Explicit, NonParallelizable]
internal sealed class RecyclarrSyncTests
{
    private static readonly FileSystem FileSystem = new();
    private static IContainer? _sonarrContainer;
    private static IContainer? _radarrContainer;
    private static ServarrTestClient _sonarr = null!;
    private static ServarrTestClient _radarr = null!;
    private static string _recyclarrBinaryPath = string.Empty;
    private static IDirectoryInfo _tempAppDataDir = null!;
    private static string _configPath = string.Empty;
    private static string _configPathDeleteDisabled = string.Empty;

    [OneTimeSetUp]
    public static async Task OneTimeSetUp()
    {
        var ct = TestContext.CurrentContext.CancellationToken;

        const string apiKey = "testkey";

        var guid = Guid.NewGuid();
        var publishPath = FileSystem.DirectoryInfo.New(
            FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), $"recyclarr-e2e-publish-{guid}")
        );
        _recyclarrBinaryPath = publishPath.File("recyclarr").FullName;
        _tempAppDataDir = FileSystem.DirectoryInfo.New(
            FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), $"recyclarr-e2e-appdata-{guid}")
        );
        _tempAppDataDir.Create();

        _configPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Fixtures",
            "recyclarr.yml"
        );

        _configPathDeleteDisabled = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Fixtures",
            "recyclarr-delete-disabled.yml"
        );

        await SetUpFixtures(ct);

        var repositoryRoot = GetRepositoryRoot();
        var cliProjectPath = Path.Combine(repositoryRoot, "src", "Recyclarr.Cli");

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

        var sonarrUrl = $"http://localhost:{_sonarrContainer.GetMappedPublicPort(8989)}";
        var radarrUrl = $"http://localhost:{_radarrContainer.GetMappedPublicPort(7878)}";

        _sonarr = new ServarrTestClient(sonarrUrl, apiKey);
        _radarr = new ServarrTestClient(radarrUrl, apiKey);
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

    [Test, Order(1)]
    [CancelAfter(60_000)]
    public async Task Order1_initial_sync_creates_expected_state(CancellationToken ct)
    {
        var result = await RunRecyclarrSync(ct);
        await LogOutput(result);
        result
            .ExitCode.Should()
            .Be(0, $"recyclarr sync failed:\n{result.StandardOutput}\n{result.StandardError}");

        await WaitForQualityDefinitionUpdates(ct);

        await VerifySonarrState(ct);
        await VerifyRadarrState(ct);
    }

    [Test, Order(2)]
    [CancelAfter(60_000)]
    public async Task Order2_resync_is_idempotent(CancellationToken ct)
    {
        var result = await RunRecyclarrSync(ct);
        await LogOutput(result);
        result.ExitCode.Should().Be(0, "re-sync should succeed");

        var sonarrCfs = await _sonarr.GetCustomFormats(ct);
        sonarrCfs
            .Select(cf => cf.Name)
            .Should()
            .BeEquivalentTo(
                [
                    "Bad Dual Groups",
                    "No-RlsGroup",
                    "Obfuscated",
                    "E2E-SonarrCustom",
                    "E2E-SonarrOverride",
                    "E2E-GroupCF1",
                    "E2E-GroupCF2",
                ],
                "CFs should be unchanged after re-sync"
            );

        var radarrCfs = await _radarr.GetCustomFormats(ct);
        radarrCfs
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
                    "Repack/Proper",
                    "LQ",
                    "E2E-GroupCF1",
                    "E2E-GroupCF2",
                ],
                "CFs should be unchanged after re-sync"
            );
    }

    [Test, Order(3)]
    [CancelAfter(60_000)]
    public async Task Order3_resync_restores_renamed_custom_format(CancellationToken ct)
    {
        // Rename a CF directly in Sonarr (simulates manual edit or service-side change)
        var sonarrCfs = await _sonarr.GetCustomFormats(ct);
        var obfuscatedCf = sonarrCfs.First(cf => cf.Name == "Obfuscated");
        await _sonarr.RenameCustomFormat(obfuscatedCf.Id, "Obfuscated-RENAMED", ct);

        // Verify rename took effect
        var renamedCfs = await _sonarr.GetCustomFormats(ct);
        renamedCfs.Should().Contain(cf => cf.Name == "Obfuscated-RENAMED");

        // Run sync - should restore original name via ID-first matching
        var result = await RunRecyclarrSync(ct);
        await LogOutput(result);
        result.ExitCode.Should().Be(0, "sync should succeed and restore renamed CF");

        // Verify name restored
        var restoredCfs = await _sonarr.GetCustomFormats(ct);
        restoredCfs
            .Should()
            .Contain(
                cf => cf.Name == "Obfuscated",
                "CF name should be restored via ID-first matching"
            );
        restoredCfs.Should().NotContain(cf => cf.Name == "Obfuscated-RENAMED");
    }

    [Test, Order(4)]
    [CancelAfter(60_000)]
    public async Task Order4_resync_recreates_deleted_custom_format(CancellationToken ct)
    {
        // Delete a CF directly in Sonarr (simulates accidental deletion)
        var sonarrCfs = await _sonarr.GetCustomFormats(ct);
        var badDualCf = sonarrCfs.First(cf => cf.Name == "Bad Dual Groups");
        await _sonarr.DeleteCustomFormat(badDualCf.Id, ct);

        // Verify deletion
        var afterDelete = await _sonarr.GetCustomFormats(ct);
        afterDelete.Should().NotContain(cf => cf.Name == "Bad Dual Groups");

        // Run sync - should detect stale cache and recreate CF
        var result = await RunRecyclarrSync(ct);
        await LogOutput(result);
        result.ExitCode.Should().Be(0, "sync should succeed and recreate deleted CF");

        // Verify CF recreated
        var recreated = await _sonarr.GetCustomFormats(ct);
        recreated
            .Should()
            .Contain(cf => cf.Name == "Bad Dual Groups", "deleted CF should be recreated");
    }

    [Test, Order(5)]
    [CancelAfter(60_000)]
    public async Task Order5_resync_preserves_orphaned_cf_when_delete_disabled(CancellationToken ct)
    {
        // Run sync with alternate config that has Obfuscated removed and delete_old_custom_formats: false
        var result = await RunRecyclarrSync(ct, _configPathDeleteDisabled);
        await LogOutput(result);
        result.ExitCode.Should().Be(0, "sync should succeed with delete disabled");

        // Verify Obfuscated still exists even though it's not in the config
        // This proves the delete toggle prevents deletion of orphaned CFs
        var sonarrCfs = await _sonarr.GetCustomFormats(ct);
        sonarrCfs
            .Should()
            .Contain(
                cf => cf.Name == "Obfuscated",
                "orphaned CF should be preserved when delete_old_custom_formats is false"
            );
    }

    private static async Task<BufferedCommandResult> RunRecyclarrSync(
        CancellationToken ct,
        string? configPath = null
    )
    {
        return await Cli.Wrap(_recyclarrBinaryPath)
            .WithArguments(["sync", "--log", "debug", "--config", configPath ?? _configPath])
            .WithEnvironmentVariables(env =>
                env.Set(
                        "SONARR_URL",
                        $"http://localhost:{_sonarrContainer!.GetMappedPublicPort(8989)}"
                    )
                    .Set("SONARR_API_KEY", "testkey")
                    .Set(
                        "RADARR_URL",
                        $"http://localhost:{_radarrContainer!.GetMappedPublicPort(7878)}"
                    )
                    .Set("RADARR_API_KEY", "testkey")
                    .Set("RECYCLARR_APP_DATA", _tempAppDataDir.FullName)
            )
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(ct);
    }

    private static async Task LogOutput(BufferedCommandResult result)
    {
        var testName = TestContext.CurrentContext.Test.Name;
        await TestContext.Out.WriteLineAsync(
            $"=== [{testName}] Recyclarr stdout ===\n{result.StandardOutput}"
        );
        await TestContext.Out.WriteLineAsync(
            $"=== [{testName}] Recyclarr stderr ===\n{result.StandardError}"
        );
    }

    private static async Task VerifySonarrState(CancellationToken ct)
    {
        var profiles = await _sonarr.GetQualityProfiles(ct);

        // User-defined quality profile
        profiles.Select(p => p.Name).Should().Contain("HD-1080p");
        profiles.First(p => p.Name == "HD-1080p").MinUpgradeFormatScore.Should().Be(100);

        // Guide-synced quality profile with config overrides
        var guideSonarrProfile = profiles.FirstOrDefault(p => p.Name == "E2E-SonarrGuideOverride");
        guideSonarrProfile
            .Should()
            .NotBeNull("guide-synced profile should exist with overridden name");
        guideSonarrProfile
            .MinUpgradeFormatScore.Should()
            .Be(150, "config override should take precedence");
        guideSonarrProfile
            .UpgradeAllowed.Should()
            .BeTrue("guide value should be preserved when not overridden");

        // Guide-only profile (tests pure inheritance - no config overrides)
        var guideOnlyProfile = profiles.FirstOrDefault(p => p.Name == "E2E-GuideOnlyProfile");
        guideOnlyProfile.Should().NotBeNull("guide-only profile should exist with guide name");
        guideOnlyProfile
            .MinUpgradeFormatScore.Should()
            .Be(25, "guide value should be used when no config override");
        guideOnlyProfile.MinFormatScore.Should().Be(10, "guide minFormatScore should be inherited");
        guideOnlyProfile.UpgradeAllowed.Should().BeTrue("guide upgradeAllowed should be inherited");

        var customFormats = await _sonarr.GetCustomFormats(ct);
        customFormats
            .Select(cf => cf.Name)
            .Should()
            .BeEquivalentTo(
                // From YAML custom_formats section
                "Bad Dual Groups",
                "No-RlsGroup",
                "Obfuscated",
                "E2E-SonarrCustom",
                "E2E-SonarrOverride",
                // From CF group (implicit assignment to all guide-backed profiles)
                "E2E-GroupCF1",
                "E2E-GroupCF2"
            );

        var qualityDefs = await _sonarr.GetQualityDefinitions(ct);
        var hdtv1080P = qualityDefs.First(q => q.Title == "HDTV-1080p");
        hdtv1080P.MinSize.Should().Be(5);
        hdtv1080P.MaxSize.Should().Be(50);
        hdtv1080P.PreferredSize.Should().Be(30);

        var bluray1080P = qualityDefs.First(q => q.Title == "Bluray-1080p");
        bluray1080P.MinSize.Should().Be(50.4m);
    }

    private static async Task VerifyRadarrState(CancellationToken ct)
    {
        var profiles = await _radarr.GetQualityProfiles(ct);

        // User-defined quality profile
        profiles.Select(p => p.Name).Should().Contain("HD-1080p");
        profiles.First(p => p.Name == "HD-1080p").MinUpgradeFormatScore.Should().Be(200);

        // Guide-synced quality profile with config overrides
        var guideRadarrProfile = profiles.FirstOrDefault(p => p.Name == "E2E-RadarrGuideOverride");
        guideRadarrProfile
            .Should()
            .NotBeNull("guide-synced profile should exist with overridden name");
        guideRadarrProfile
            .MinUpgradeFormatScore.Should()
            .Be(250, "config override should take precedence");
        guideRadarrProfile
            .UpgradeAllowed.Should()
            .BeFalse("config override should take precedence over guide value");
        guideRadarrProfile
            .Language?.Name.Should()
            .Be("English", "language from guide resource should be applied");

        var customFormats = await _radarr.GetCustomFormats(ct);
        customFormats
            .Select(cf => cf.Name)
            .Should()
            .BeEquivalentTo(
                // From YAML custom_formats section
                "Hybrid-OVERRIDE",
                "Remaster",
                "4K Remaster",
                "Criterion Collection",
                "Masters of Cinema",
                "Vinegar Syndrome",
                "Special Edition",
                "E2E-TestFormat",
                // From QP formatItems (not in YAML custom_formats, synced via guide QP)
                "Repack/Proper",
                "LQ",
                // From CF group (explicit assignment to guide profile)
                "E2E-GroupCF1",
                "E2E-GroupCF2"
            );

        var qualityDefs = await _radarr.GetQualityDefinitions(ct);
        var bluray1080P = qualityDefs.First(q => q.Title == "Bluray-1080p");
        bluray1080P.MaxSize.Should().BeNull();
        bluray1080P.PreferredSize.Should().BeNull();

        var hdtv1080P = qualityDefs.First(q => q.Title == "HDTV-1080p");
        hdtv1080P.MinSize.Should().Be(0);
        hdtv1080P.MaxSize.Should().Be(100);
        hdtv1080P.PreferredSize.Should().Be(50);

        var webdl1080P = qualityDefs.First(q => q.Title == "WEBDL-1080p");
        webdl1080P.MinSize.Should().Be(12.5m);
    }

    private static async Task WaitForQualityDefinitionUpdates(CancellationToken ct)
    {
        var timeout = TimeSpan.FromSeconds(10);
        var pollInterval = TimeSpan.FromMilliseconds(500);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            var sonarrDefs = await _sonarr.GetQualityDefinitions(ct);
            var sonarrHdtv = sonarrDefs.FirstOrDefault(q => q.Title == "HDTV-1080p");
            if (sonarrHdtv?.MinSize == 5)
            {
                break;
            }

            await Task.Delay(pollInterval, ct);
        }

        while (DateTime.UtcNow < deadline)
        {
            var radarrDefs = await _radarr.GetQualityDefinitions(ct);
            var radarrHdtv = radarrDefs.FirstOrDefault(q => q.Title == "HDTV-1080p");
            if (radarrHdtv?.MinSize == 0)
            {
                break;
            }

            await Task.Delay(pollInterval, ct);
        }
    }

    private static async Task SetUpFixtures(CancellationToken ct)
    {
        var fixturesDir = FileSystem.DirectoryInfo.New(
            FileSystem.Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures")
        );

        var sonarrCfsSource = fixturesDir.SubDirectory("custom-formats-sonarr");
        var sonarrCfsDest = _tempAppDataDir.SubDirectory("custom-formats-sonarr");
        if (sonarrCfsSource.Exists)
        {
            CopyDirectory(sonarrCfsSource, sonarrCfsDest);
        }

        var radarrCfsSource = fixturesDir.SubDirectory("custom-formats-radarr");
        var radarrCfsDest = _tempAppDataDir.SubDirectory("custom-formats-radarr");
        if (radarrCfsSource.Exists)
        {
            CopyDirectory(radarrCfsSource, radarrCfsDest);
        }

        var overrideSource = fixturesDir.SubDirectory("trash-guides-override");
        var overrideDest = _tempAppDataDir.SubDirectory("trash-guides-override");
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
            var settingsDest = _tempAppDataDir.File("settings.yml");
            await FileSystem.File.WriteAllTextAsync(settingsDest.FullName, settingsContent, ct);
        }
    }

    private static string GetRepositoryRoot()
    {
        var assembly = typeof(RecyclarrSyncTests).Assembly;
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
