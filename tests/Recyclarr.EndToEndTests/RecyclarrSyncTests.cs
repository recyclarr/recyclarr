using System.IO.Abstractions;
using AwesomeAssertions;
using CliWrap;
using CliWrap.Buffered;
using Recyclarr.EndToEndTests.Fixtures;

namespace Recyclarr.EndToEndTests;

[NotInParallel]
[Category("E2E")]
internal sealed class RecyclarrSyncTests
{
    [ClassDataSource<SonarrContainer>(Shared = SharedType.PerTestSession)]
    public required SonarrContainer Sonarr { get; init; }

    [ClassDataSource<RadarrContainer>(Shared = SharedType.PerTestSession)]
    public required RadarrContainer Radarr { get; init; }

    [ClassDataSource<RecyclarrBinary>(Shared = SharedType.PerTestSession)]
    public required RecyclarrBinary Binary { get; init; }

    [ClassDataSource<E2EAppData>(Shared = SharedType.PerTestSession)]
    public required E2EAppData AppData { get; init; }

    [Test]
    [Timeout(60_000)]
    public async Task Initial_sync_creates_expected_state(CancellationToken ct)
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

    [Test]
    [Timeout(60_000)]
    [DependsOn(nameof(Initial_sync_creates_expected_state))]
    public async Task Resync_is_idempotent(CancellationToken ct)
    {
        var result = await RunRecyclarrSync(ct);
        await LogOutput(result);
        result.ExitCode.Should().Be(0, "re-sync should succeed");

        var sonarrCfs = await Sonarr.Client.GetCustomFormats(ct);
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

        var radarrCfs = await Radarr.Client.GetCustomFormats(ct);
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

    [Test]
    [Timeout(60_000)]
    [DependsOn(nameof(Resync_is_idempotent))]
    public async Task Resync_restores_renamed_custom_format(CancellationToken ct)
    {
        // Rename a CF directly in Sonarr (simulates manual edit or service-side change)
        var sonarrCfs = await Sonarr.Client.GetCustomFormats(ct);
        var obfuscatedCf = sonarrCfs.First(cf => cf.Name == "Obfuscated");
        await Sonarr.Client.RenameCustomFormat(obfuscatedCf.Id, "Obfuscated-RENAMED", ct);

        // Verify rename took effect
        var renamedCfs = await Sonarr.Client.GetCustomFormats(ct);
        renamedCfs.Should().Contain(cf => cf.Name == "Obfuscated-RENAMED");

        // Run sync - should restore original name via ID-first matching
        var result = await RunRecyclarrSync(ct);
        await LogOutput(result);
        result.ExitCode.Should().Be(0, "sync should succeed and restore renamed CF");

        // Verify name restored
        var restoredCfs = await Sonarr.Client.GetCustomFormats(ct);
        restoredCfs
            .Should()
            .Contain(
                cf => cf.Name == "Obfuscated",
                "CF name should be restored via ID-first matching"
            );
        restoredCfs.Should().NotContain(cf => cf.Name == "Obfuscated-RENAMED");
    }

    [Test]
    [Timeout(60_000)]
    [DependsOn(nameof(Resync_restores_renamed_custom_format))]
    public async Task Resync_recreates_deleted_custom_format(CancellationToken ct)
    {
        // Delete a CF directly in Sonarr (simulates accidental deletion)
        var sonarrCfs = await Sonarr.Client.GetCustomFormats(ct);
        var badDualCf = sonarrCfs.First(cf => cf.Name == "Bad Dual Groups");
        await Sonarr.Client.DeleteCustomFormat(badDualCf.Id, ct);

        // Verify deletion
        var afterDelete = await Sonarr.Client.GetCustomFormats(ct);
        afterDelete.Should().NotContain(cf => cf.Name == "Bad Dual Groups");

        // Run sync - should detect stale cache and recreate CF
        var result = await RunRecyclarrSync(ct);
        await LogOutput(result);
        result.ExitCode.Should().Be(0, "sync should succeed and recreate deleted CF");

        // Verify CF recreated
        var recreated = await Sonarr.Client.GetCustomFormats(ct);
        recreated
            .Should()
            .Contain(cf => cf.Name == "Bad Dual Groups", "deleted CF should be recreated");
    }

    [Test]
    [Timeout(60_000)]
    [DependsOn(nameof(Resync_recreates_deleted_custom_format))]
    public async Task Resync_preserves_orphaned_cf_when_delete_disabled(CancellationToken ct)
    {
        // Run sync with alternate config that has Obfuscated removed and delete_old_custom_formats: false
        var result = await RunRecyclarrSync(ct, AppData.ConfigFileDeleteDisabled);
        await LogOutput(result);
        result.ExitCode.Should().Be(0, "sync should succeed with delete disabled");

        // Verify Obfuscated still exists even though it's not in the config.
        // This proves the delete toggle prevents deletion of orphaned CFs.
        var sonarrCfs = await Sonarr.Client.GetCustomFormats(ct);
        sonarrCfs
            .Should()
            .Contain(
                cf => cf.Name == "Obfuscated",
                "orphaned CF should be preserved when delete_old_custom_formats is false"
            );
    }

    private async Task<BufferedCommandResult> RunRecyclarrSync(
        CancellationToken ct,
        IFileInfo? configFile = null
    )
    {
        var config = configFile ?? AppData.ConfigFile;
        return await Cli.Wrap(Binary.Binary.FullName)
            .WithArguments(["sync", "--log", "debug", "--config", config.FullName])
            .WithEnvironmentVariables(env =>
                env.Set("SONARR_URL", Sonarr.BaseUrl)
                    .Set("SONARR_API_KEY", "testkey")
                    .Set("RADARR_URL", Radarr.BaseUrl)
                    .Set("RADARR_API_KEY", "testkey")
                    .Set("RECYCLARR_CONFIG_DIR", AppData.AppDataDir.FullName)
                    .Set("RECYCLARR_APP_DATA", "")
            )
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(ct);
    }

    private static async Task LogOutput(BufferedCommandResult result)
    {
        var testName = TestContext.Current?.Metadata.TestDetails.TestName ?? "Unknown";
        await Console.Out.WriteLineAsync(
            $"=== [{testName}] Recyclarr stdout ===\n{result.StandardOutput}"
        );
        await Console.Out.WriteLineAsync(
            $"=== [{testName}] Recyclarr stderr ===\n{result.StandardError}"
        );
    }

    private async Task VerifySonarrState(CancellationToken ct)
    {
        var profiles = await Sonarr.Client.GetQualityProfiles(ct);

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

        var customFormats = await Sonarr.Client.GetCustomFormats(ct);
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

        var qualityDefs = await Sonarr.Client.GetQualityDefinitions(ct);
        var hdtv1080P = qualityDefs.First(q => q.Title == "HDTV-1080p");
        hdtv1080P.MinSize.Should().Be(5);
        hdtv1080P.MaxSize.Should().Be(50);
        hdtv1080P.PreferredSize.Should().Be(30);

        var bluray1080P = qualityDefs.First(q => q.Title == "Bluray-1080p");
        bluray1080P.MinSize.Should().Be(50.4m);

        // Media management settings
        var mediaManagement = await Sonarr.Client.GetMediaManagement(ct);
        mediaManagement
            .DownloadPropersAndRepacks.Should()
            .Be("doNotUpgrade", "propers_and_repacks should be set to do_not_upgrade");
    }

    private async Task VerifyRadarrState(CancellationToken ct)
    {
        var profiles = await Radarr.Client.GetQualityProfiles(ct);

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

        var customFormats = await Radarr.Client.GetCustomFormats(ct);
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

        var qualityDefs = await Radarr.Client.GetQualityDefinitions(ct);
        var bluray1080P = qualityDefs.First(q => q.Title == "Bluray-1080p");
        bluray1080P.MaxSize.Should().BeNull();
        bluray1080P.PreferredSize.Should().BeNull();

        var hdtv1080P = qualityDefs.First(q => q.Title == "HDTV-1080p");
        hdtv1080P.MinSize.Should().Be(0);
        hdtv1080P.MaxSize.Should().Be(100);
        hdtv1080P.PreferredSize.Should().Be(50);

        var webdl1080P = qualityDefs.First(q => q.Title == "WEBDL-1080p");
        webdl1080P.MinSize.Should().Be(12.5m);

        // Media management settings
        var mediaManagement = await Radarr.Client.GetMediaManagement(ct);
        mediaManagement
            .DownloadPropersAndRepacks.Should()
            .Be("doNotPrefer", "propers_and_repacks should be set to do_not_prefer");
    }

    private async Task WaitForQualityDefinitionUpdates(CancellationToken ct)
    {
        var timeout = TimeSpan.FromSeconds(10);
        var pollInterval = TimeSpan.FromMilliseconds(500);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            var sonarrDefs = await Sonarr.Client.GetQualityDefinitions(ct);
            var sonarrHdtv = sonarrDefs.FirstOrDefault(q => q.Title == "HDTV-1080p");
            if (sonarrHdtv?.MinSize == 5)
            {
                break;
            }

            await Task.Delay(pollInterval, ct);
        }

        while (DateTime.UtcNow < deadline)
        {
            var radarrDefs = await Radarr.Client.GetQualityDefinitions(ct);
            var radarrHdtv = radarrDefs.FirstOrDefault(q => q.Title == "HDTV-1080p");
            if (radarrHdtv?.MinSize == 0)
            {
                break;
            }

            await Task.Delay(pollInterval, ct);
        }
    }
}
