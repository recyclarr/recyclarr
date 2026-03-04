using System.Globalization;
using AwesomeAssertions;
using CliWrap;
using CliWrap.Buffered;
using NUnit.Framework;
using RadarrApi = Recyclarr.Api.Radarr;
using SonarrApi = Recyclarr.Api.Sonarr;

namespace Recyclarr.EndToEndTests;

[TestFixture(Category = "E2E"), Explicit, NonParallelizable]
internal sealed class RecyclarrSyncTests
{
    private static RecyclarrTestHarness _harness = null!;

    [OneTimeSetUp]
    public static async Task OneTimeSetUp()
    {
        _harness = await RecyclarrTestHarness.StartAsync(TestContext.CurrentContext);
    }

    [OneTimeTearDown]
    public static async Task OneTimeTearDown()
    {
        await _harness.DisposeAsync();
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

        var sonarrCfNames = (
            await _harness.SonarrApi<SonarrApi.ICustomFormatApi>().CustomformatGet(ct)
        ).Select(cf => cf.Name);
        sonarrCfNames
            .Should()
            .Contain(
                ["Bad Dual Groups", "No-RlsGroup", "Obfuscated", "E2E-SonarrCustom"],
                "CFs should be unchanged after re-sync"
            );

        var radarrCfNames = (
            await _harness.RadarrApi<RadarrApi.ICustomFormatApi>().CustomformatGet(ct)
        )
            .Select(cf => cf.Name)
            .ToList();
        radarrCfNames
            .Should()
            .Contain(
                [
                    "Hybrid-OVERRIDE",
                    "E2E-TestFormat",
                    "E2E-GroupCF1",
                    "E2E-GroupCF3",
                    "x265 (HD)",
                    "AMZN",
                ],
                "CFs should be unchanged after re-sync"
            );
        radarrCfNames
            .Should()
            .NotContain("E2E-GroupCF2", "excluded CF should remain absent after re-sync");
    }

    [Test, Order(3)]
    [CancelAfter(60_000)]
    public async Task Order3_resync_restores_renamed_custom_format(CancellationToken ct)
    {
        var cfApi = _harness.SonarrApi<SonarrApi.ICustomFormatApi>();

        // Rename a CF directly in Sonarr (simulates manual edit or service-side change)
        var sonarrCfs = await cfApi.CustomformatGet(ct);
        var obfuscatedId = sonarrCfs.First(cf => cf.Name == "Obfuscated").Id!.Value;
        // Fetch full resource by ID (list endpoint returns minimal data insufficient for PUT)
        var obfuscatedCf = await cfApi.CustomformatGet(obfuscatedId, ct);
        obfuscatedCf.Name = "Obfuscated-RENAMED";
        await cfApi.CustomformatPut(
            obfuscatedId.ToString(CultureInfo.InvariantCulture),
            obfuscatedCf,
            ct
        );

        // Verify rename took effect
        var renamedCfs = await cfApi.CustomformatGet(ct);
        renamedCfs.Should().Contain(cf => cf.Name == "Obfuscated-RENAMED");

        // Run sync - should restore original name via ID-first matching
        var result = await RunRecyclarrSync(ct);
        await LogOutput(result);
        result.ExitCode.Should().Be(0, "sync should succeed and restore renamed CF");

        // Verify name restored
        var restoredCfs = await cfApi.CustomformatGet(ct);
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
        var cfApi = _harness.SonarrApi<SonarrApi.ICustomFormatApi>();

        // Delete a CF directly in Sonarr (simulates accidental deletion)
        var sonarrCfs = await cfApi.CustomformatGet(ct);
        var badDualCf = sonarrCfs.First(cf => cf.Name == "Bad Dual Groups");
        await cfApi.CustomformatDelete(badDualCf.Id!.Value, ct);

        // Verify deletion
        var afterDelete = await cfApi.CustomformatGet(ct);
        afterDelete.Should().NotContain(cf => cf.Name == "Bad Dual Groups");

        // Run sync - should detect stale cache and recreate CF
        var result = await RunRecyclarrSync(ct);
        await LogOutput(result);
        result.ExitCode.Should().Be(0, "sync should succeed and recreate deleted CF");

        // Verify CF recreated
        var recreated = await cfApi.CustomformatGet(ct);
        recreated
            .Should()
            .Contain(cf => cf.Name == "Bad Dual Groups", "deleted CF should be recreated");
    }

    [Test, Order(5)]
    [CancelAfter(60_000)]
    public async Task Order5_resync_preserves_orphaned_cf_when_delete_disabled(CancellationToken ct)
    {
        // Run sync with alternate config that has Obfuscated removed and delete_old_custom_formats: false
        var result = await RunRecyclarrSync(ct, _harness.ConfigPathDeleteDisabled);
        await LogOutput(result);
        result.ExitCode.Should().Be(0, "sync should succeed with delete disabled");

        // Verify Obfuscated still exists even though it's not in the config
        // This proves the delete toggle prevents deletion of orphaned CFs
        var sonarrCfs = await _harness.SonarrApi<SonarrApi.ICustomFormatApi>().CustomformatGet(ct);
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
        return await Cli.Wrap(_harness.RecyclarrBinaryPath)
            .WithArguments([
                "sync",
                "--log",
                "debug",
                "--config",
                configPath ?? _harness.ConfigPath,
            ])
            .WithEnvironmentVariables(env =>
                env.Set("SONARR_URL", $"http://localhost:{_harness.SonarrPort}")
                    .Set("SONARR_API_KEY", "testkey")
                    .Set("RADARR_URL", $"http://localhost:{_harness.RadarrPort}")
                    .Set("RADARR_API_KEY", "testkey")
                    .Set("RECYCLARR_CONFIG_DIR", _harness.AppDataDir.FullName)
                    .Set("RECYCLARR_APP_DATA", "")
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
        var profiles = await _harness
            .SonarrApi<SonarrApi.IQualityProfileApi>()
            .QualityprofileGet(ct);

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

        var sonarrCfNames = (
            await _harness.SonarrApi<SonarrApi.ICustomFormatApi>().CustomformatGet(ct)
        ).Select(cf => cf.Name);
        string[] expectedSonarrCfs =
        [
            // From YAML custom_formats section
            "Bad Dual Groups",
            "No-RlsGroup",
            "Obfuscated",
            "E2E-SonarrCustom",
            "E2E-SonarrOverride",
            // From CF group (implicit assignment to all guide-backed profiles)
            "E2E-GroupCF1",
            "E2E-GroupCF2",
        ];
        sonarrCfNames.Should().Contain(expectedSonarrCfs);

        var qualityDefs = await _harness
            .SonarrApi<SonarrApi.IQualityDefinitionApi>()
            .QualitydefinitionGet(ct);
        var hdtv1080P = qualityDefs.First(q => q.Title == "HDTV-1080p");
        hdtv1080P.MinSize.Should().BeApproximately(5, 0.1);
        hdtv1080P.MaxSize.Should().BeApproximately(50, 0.1);
        hdtv1080P.PreferredSize.Should().BeApproximately(30, 0.1);

        var bluray1080P = qualityDefs.First(q => q.Title == "Bluray-1080p");
        bluray1080P.MinSize.Should().BeApproximately(50.4, 0.1);

        // Media naming settings
        var sonarrNaming = await _harness.SonarrApi<SonarrApi.INamingConfigApi>().NamingGet(ct);
        sonarrNaming.RenameEpisodes.Should().BeTrue("rename should be enabled");
        sonarrNaming.SeasonFolderFormat.Should().Be("Season {season:00}");
        sonarrNaming.SeriesFolderFormat.Should().Be("{Series TitleYear}");
        sonarrNaming
            .StandardEpisodeFormat.Should()
            .Contain("S{season:00}E{episode:00}", "standard episode format should be set");
        sonarrNaming
            .DailyEpisodeFormat.Should()
            .Contain("{Air-Date}", "daily episode format should be set");
        sonarrNaming
            .AnimeEpisodeFormat.Should()
            .Contain("{absolute:000}", "anime episode format should be set");

        // Media management settings
        var mediaManagement = await _harness
            .SonarrApi<SonarrApi.IMediaManagementConfigApi>()
            .MediamanagementGet(ct);
        mediaManagement
            .DownloadPropersAndRepacks.Should()
            .Be(
                SonarrApi.ProperDownloadTypes.DoNotUpgrade,
                "propers_and_repacks should be set to do_not_upgrade"
            );
    }

    private static async Task VerifyRadarrState(CancellationToken ct)
    {
        var profiles = await _harness
            .RadarrApi<RadarrApi.IQualityProfileApi>()
            .QualityprofileGet(ct);

        // User-defined quality profile
        var hdProfile = profiles.First(p => p.Name == "HD-1080p");
        hdProfile.MinUpgradeFormatScore.Should().Be(200);

        // CF group CFs should be scored on HD-1080p (tests name-based assign_scores_to)
        // CF1 (required) + CF3 (selected) are scored; CF2 (excluded) is not
        var hdScoredCfNames = hdProfile
            .FormatItems!.Where(fi => fi.Score != 0)
            .Select(fi => fi.Name)
            .ToList();
        hdScoredCfNames
            .Should()
            .Contain(
                ["E2E-GroupCF1", "E2E-GroupCF3"],
                "CF group: required + selected CFs should be scored via name-based assignment"
            );
        hdScoredCfNames
            .Should()
            .NotContain("E2E-GroupCF2", "CF group: excluded default CF should not be scored");

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

        // Guide-synced quality profile (tests implicit CF group inclusion)
        var hdBlurayProfile = profiles.FirstOrDefault(p => p.Name == "HD Bluray + WEB");
        hdBlurayProfile.Should().NotBeNull("guide HD Bluray + WEB profile should be created");
        hdBlurayProfile.UpgradeAllowed.Should().BeTrue("guide value should be inherited");

        var radarrCfNames = (
            await _harness.RadarrApi<RadarrApi.ICustomFormatApi>().CustomformatGet(ct)
        )
            .Select(cf => cf.Name)
            .ToList();
        string[] expectedRadarrCfs =
        [
            // From YAML custom_formats section
            "Hybrid-OVERRIDE",
            "Remaster",
            "4K Remaster",
            "Criterion Collection",
            "Masters of Cinema",
            "Vinegar Syndrome",
            "Special Edition",
            "E2E-TestFormat",
            // From E2E override QP formatItems
            "Repack/Proper",
            "LQ",
            // From E2E CF group: CF1 (required) + CF3 (selected); CF2 excluded
            "E2E-GroupCF1",
            "E2E-GroupCF3",
            // From default guide CF groups implicitly included via HD Bluray + WEB profile
            "x265 (HD)", // [Required] Golden Rule HD
            "AMZN", // [Streaming Services] General
        ];
        radarrCfNames.Should().Contain(expectedRadarrCfs);
        radarrCfNames
            .Should()
            .NotContain("E2E-GroupCF2", "excluded default CF should not be synced");

        var qualityDefs = await _harness
            .RadarrApi<RadarrApi.IQualityDefinitionApi>()
            .QualitydefinitionGet(ct);
        var bluray1080P = qualityDefs.First(q => q.Title == "Bluray-1080p");
        bluray1080P.MaxSize.Should().BeNull();
        bluray1080P.PreferredSize.Should().BeNull();

        var hdtv1080P = qualityDefs.First(q => q.Title == "HDTV-1080p");
        hdtv1080P.MinSize.Should().BeApproximately(0, 0.1);
        hdtv1080P.MaxSize.Should().BeApproximately(100, 0.1);
        hdtv1080P.PreferredSize.Should().BeApproximately(50, 0.1);

        var webdl1080P = qualityDefs.First(q => q.Title == "WEBDL-1080p");
        webdl1080P.MinSize.Should().BeApproximately(12.5, 0.1);

        // Media naming settings
        var radarrNaming = await _harness.RadarrApi<RadarrApi.INamingConfigApi>().NamingGet(ct);
        radarrNaming.RenameMovies.Should().BeTrue("rename should be enabled");
        radarrNaming
            .MovieFolderFormat.Should()
            .Be(
                "{Movie CleanTitle} ({Release Year})",
                "folder format should match guide 'default'"
            );
        radarrNaming
            .StandardMovieFormat.Should()
            .Contain("{Movie CleanTitle}", "standard movie format should be set from guide");

        // Media management settings
        var mediaManagement = await _harness
            .RadarrApi<RadarrApi.IMediaManagementConfigApi>()
            .MediamanagementGet(ct);
        mediaManagement
            .DownloadPropersAndRepacks.Should()
            .Be(
                RadarrApi.ProperDownloadTypes.DoNotPrefer,
                "propers_and_repacks should be set to do_not_prefer"
            );
    }

    private static async Task WaitForQualityDefinitionUpdates(CancellationToken ct)
    {
        var timeout = TimeSpan.FromSeconds(10);
        var pollInterval = TimeSpan.FromMilliseconds(500);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            var sonarrDefs = await _harness
                .SonarrApi<SonarrApi.IQualityDefinitionApi>()
                .QualitydefinitionGet(ct);
            var sonarrHdtv = sonarrDefs.FirstOrDefault(q => q.Title == "HDTV-1080p");
            if (sonarrHdtv?.MinSize is >= 4.9 and <= 5.1)
            {
                break;
            }

            await Task.Delay(pollInterval, ct);
        }

        while (DateTime.UtcNow < deadline)
        {
            var radarrDefs = await _harness
                .RadarrApi<RadarrApi.IQualityDefinitionApi>()
                .QualitydefinitionGet(ct);
            var radarrHdtv = radarrDefs.FirstOrDefault(q => q.Title == "HDTV-1080p");
            if (radarrHdtv?.MinSize is >= -0.1 and <= 0.1)
            {
                break;
            }

            await Task.Delay(pollInterval, ct);
        }
    }
}
