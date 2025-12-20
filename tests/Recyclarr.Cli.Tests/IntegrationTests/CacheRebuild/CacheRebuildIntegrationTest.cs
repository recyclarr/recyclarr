using System.IO.Abstractions;
using System.Text.Json;
using Autofac;
using Recyclarr.Cache;
using Recyclarr.Cli.Console;
using Recyclarr.Cli.Pipelines.CustomFormat.Cache;
using Recyclarr.Cli.Tests.Reusable;
using Recyclarr.Config.Parsing;
using Recyclarr.Core.TestLibrary;
using Recyclarr.Json;
using Recyclarr.ResourceProviders.Domain;
using Recyclarr.ServarrApi.CustomFormat;

namespace Recyclarr.Cli.Tests.IntegrationTests.CacheRebuild;

internal sealed class CacheRebuildIntegrationTest : CliIntegrationFixture
{
    private ICustomFormatApiService _apiService = null!;

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);

        _apiService = Substitute.For<ICustomFormatApiService>();
        builder.RegisterInstance(_apiService).As<ICustomFormatApiService>();
    }

    private void SetupGuideCfs(string serviceType, params (string TrashId, string Name)[] cfs)
    {
        var cfDir = Paths
            .ReposDirectory.SubDirectory("trash-guides")
            .SubDirectory("git")
            .SubDirectory("official")
            .SubDirectory("docs")
            .SubDirectory("json")
            .SubDirectory(serviceType.ToLowerInvariant())
            .SubDirectory("cf");

        foreach (var (trashId, name) in cfs)
        {
            var cfJson = $$"""
                {
                    "trash_id": "{{trashId}}",
                    "name": "{{name}}",
                    "includeCustomFormatWhenRenaming": false,
                    "specifications": []
                }
                """;
            Fs.AddFile(cfDir.File($"{trashId}.json"), new MockFileData(cfJson));
        }
    }

    private void SetupRadarrConfig(string instanceName, params string[] trashIds)
    {
        var config = new RootConfigYaml
        {
            Radarr = new Dictionary<string, RadarrConfigYaml?>
            {
                [instanceName] = new()
                {
                    BaseUrl = "http://localhost:8989",
                    ApiKey = "test-api-key",
                    CustomFormats =
                        trashIds.Length > 0
                            ? [new CustomFormatConfigYaml { TrashIds = trashIds }]
                            : null,
                },
            },
        };
        Fs.AddYamlFile(Paths.ConfigsDirectory.File("config.yml"), config);
    }

    private void SetupServiceCfs(params CustomFormatResource[] cfs)
    {
        _apiService.GetCustomFormats(Arg.Any<CancellationToken>()).Returns(cfs.ToList());
    }

    [Test]
    public async Task Rebuild_matches_cfs_by_name_case_insensitive()
    {
        SetupRadarrConfig("test-instance", "trash-id-1", "trash-id-2");
        SetupGuideCfs(
            "radarr",
            ("trash-id-1", "Custom Format One"),
            ("trash-id-2", "Custom Format Two")
        );
        SetupServiceCfs(
            new CustomFormatResource { Id = 10, Name = "Custom Format One" },
            new CustomFormatResource { Id = 20, Name = "custom format two" } // Different case
        );

        // --adopt is required to add entries when no cache exists
        var exitCode = await CliSetup.Run(
            Container,
            ["cache", "rebuild", "-i", "test-instance", "--adopt"]
        );

        exitCode.Should().Be(0);

        // Verify cache file was created with correct mappings
        var cacheFile = GetCacheFilePath("radarr");
        Fs.File.Exists(cacheFile).Should().BeTrue();

        var cacheContent = await Fs.File.ReadAllTextAsync(cacheFile);
        var cache = JsonSerializer.Deserialize<CustomFormatCacheObject>(
            cacheContent,
            GlobalJsonSerializerSettings.Recyclarr
        );

        cache.Should().NotBeNull();
        cache
            .Mappings.Should()
            .BeEquivalentTo(
                new[]
                {
                    new { TrashId = "trash-id-1", ServiceId = 10 },
                    new { TrashId = "trash-id-2", ServiceId = 20 },
                }
            );
    }

    [Test]
    public async Task Rebuild_detects_ambiguous_matches_and_fails()
    {
        SetupRadarrConfig("test-instance", "trash-id-1");
        SetupGuideCfs("radarr", ("trash-id-1", "HULU"));
        SetupServiceCfs(
            new CustomFormatResource { Id = 10, Name = "HULU" },
            new CustomFormatResource { Id = 20, Name = "Hulu" },
            new CustomFormatResource { Id = 30, Name = "hulu" }
        );

        var exitCode = await CliSetup.Run(Container, ["cache", "rebuild", "-i", "test-instance"]);

        exitCode.Should().Be(1);

        // Verify no cache file was created due to ambiguous match
        var cacheFile = GetCacheFilePath("radarr");
        cacheFile.Should().BeEmpty();
    }

    [Test]
    public async Task Rebuild_only_caches_cfs_that_exist_in_service()
    {
        SetupRadarrConfig("test-instance", "trash-id-1", "trash-id-2");
        SetupGuideCfs(
            "radarr",
            ("trash-id-1", "Exists In Service"),
            ("trash-id-2", "Not In Service")
        );
        SetupServiceCfs(new CustomFormatResource { Id = 10, Name = "Exists In Service" });

        var exitCode = await CliSetup.Run(
            Container,
            ["cache", "rebuild", "-i", "test-instance", "--adopt"]
        );

        exitCode.Should().Be(0);

        // Verify cache only contains the CF that exists in service
        var cacheFile = GetCacheFilePath("radarr");
        Fs.File.Exists(cacheFile).Should().BeTrue();

        var cacheContent = await Fs.File.ReadAllTextAsync(cacheFile);
        var cache = JsonSerializer.Deserialize<CustomFormatCacheObject>(
            cacheContent,
            GlobalJsonSerializerSettings.Recyclarr
        );

        cache.Should().NotBeNull();
        cache
            .Mappings.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new { TrashId = "trash-id-1", ServiceId = 10 });
    }

    [Test]
    public async Task Preview_mode_does_not_save_cache()
    {
        SetupRadarrConfig("test-instance", "trash-id-1");
        SetupGuideCfs("radarr", ("trash-id-1", "Test CF"));
        SetupServiceCfs(new CustomFormatResource { Id = 10, Name = "Test CF" });

        // Existing cache with wrong ID that would be corrected in non-preview mode
        var existingCache = new CustomFormatCacheObject
        {
            Mappings = [new TrashIdMapping("trash-id-1", "Test CF", ServiceId: 99)],
        };
        SetupExistingCache("radarr", existingCache);

        var exitCode = await CliSetup.Run(
            Container,
            ["cache", "rebuild", "-i", "test-instance", "--preview"]
        );

        exitCode.Should().Be(0);

        // Verify cache unchanged (still has wrong ID, not corrected to 10)
        var cacheFile = GetCacheFilePath("radarr");
        var cacheContent = await Fs.File.ReadAllTextAsync(cacheFile);
        var cache = JsonSerializer.Deserialize<CustomFormatCacheObject>(
            cacheContent,
            GlobalJsonSerializerSettings.Recyclarr
        );

        cache.Should().NotBeNull();
        cache
            .Mappings.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new { TrashId = "trash-id-1", ServiceId = 99 });
    }

    [Test]
    public async Task Rebuild_with_explicit_custom_formats_resource_type()
    {
        SetupRadarrConfig("test-instance", "trash-id-1");
        SetupGuideCfs("radarr", ("trash-id-1", "Test CF"));
        SetupServiceCfs(new CustomFormatResource { Id = 10, Name = "Test CF" });

        // Explicit resource type argument: "custom-formats" (kebab-case)
        var exitCode = await CliSetup.Run(
            Container,
            ["cache", "rebuild", "custom-formats", "-i", "test-instance", "--adopt"]
        );

        exitCode.Should().Be(0);

        // Verify cache was created
        var cacheFile = GetCacheFilePath("radarr");
        Fs.File.Exists(cacheFile).Should().BeTrue();
    }

    [Test]
    public async Task Rebuild_preserves_non_configured_cache_entries()
    {
        // Setup: existing cache has an entry not in current config
        SetupRadarrConfig("test-instance", "trash-id-1");
        SetupGuideCfs("radarr", ("trash-id-1", "Configured CF"));
        SetupServiceCfs(
            new CustomFormatResource { Id = 10, Name = "Configured CF" },
            new CustomFormatResource { Id = 20, Name = "Previously Synced" } // Still exists in service
        );

        // Create existing cache with both entries
        var existingCache = new CustomFormatCacheObject
        {
            Mappings =
            [
                new TrashIdMapping("trash-id-1", "Configured CF", ServiceId: 10),
                new TrashIdMapping("trash-id-old", "Previously Synced", ServiceId: 20), // Not in current config
            ],
        };
        SetupExistingCache("radarr", existingCache);

        var exitCode = await CliSetup.Run(Container, ["cache", "rebuild", "-i", "test-instance"]);

        exitCode.Should().Be(0);

        // Verify cache contains both entries
        var cacheFile = GetCacheFilePath("radarr");
        var cacheContent = await Fs.File.ReadAllTextAsync(cacheFile);
        var cache = JsonSerializer.Deserialize<CustomFormatCacheObject>(
            cacheContent,
            GlobalJsonSerializerSettings.Recyclarr
        );

        cache.Should().NotBeNull();
        cache
            .Mappings.Should()
            .BeEquivalentTo(
                new[]
                {
                    new { TrashId = "trash-id-1", ServiceId = 10 },
                    new { TrashId = "trash-id-old", ServiceId = 20 },
                }
            );
    }

    [Test]
    public async Task Rebuild_removes_stale_cache_entries()
    {
        // Setup: existing cache has an entry whose service CF was deleted
        SetupRadarrConfig("test-instance", "trash-id-1");
        SetupGuideCfs("radarr", ("trash-id-1", "Configured CF"));
        SetupServiceCfs(new CustomFormatResource { Id = 10, Name = "Configured CF" });
        // Note: No service CF with ID 99

        // Create existing cache with stale entry
        var existingCache = new CustomFormatCacheObject
        {
            Mappings =
            [
                new TrashIdMapping("trash-id-1", "Configured CF", ServiceId: 10),
                new TrashIdMapping("trash-id-deleted", "Deleted CF", ServiceId: 99), // Service CF no longer exists
            ],
        };
        SetupExistingCache("radarr", existingCache);

        var exitCode = await CliSetup.Run(Container, ["cache", "rebuild", "-i", "test-instance"]);

        exitCode.Should().Be(0);

        // Verify cache only contains the valid entry (stale entry removed)
        var cacheFile = GetCacheFilePath("radarr");
        var cacheContent = await Fs.File.ReadAllTextAsync(cacheFile);
        var cache = JsonSerializer.Deserialize<CustomFormatCacheObject>(
            cacheContent,
            GlobalJsonSerializerSettings.Recyclarr
        );

        cache.Should().NotBeNull();
        cache
            .Mappings.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new { TrashId = "trash-id-1", ServiceId = 10 });
    }

    [Test]
    public async Task Rebuild_corrects_cache_entries_with_wrong_service_id()
    {
        // Setup: existing cache has wrong mapping
        SetupRadarrConfig("test-instance", "trash-id-1");
        SetupGuideCfs("radarr", ("trash-id-1", "Test CF"));
        SetupServiceCfs(new CustomFormatResource { Id = 20, Name = "Test CF" }); // ID 20, not 10

        // Create existing cache with wrong ID
        var existingCache = new CustomFormatCacheObject
        {
            Mappings = [new TrashIdMapping("trash-id-1", "Test CF", ServiceId: 10)], // Wrong ID - should be 20
        };
        SetupExistingCache("radarr", existingCache);

        var exitCode = await CliSetup.Run(Container, ["cache", "rebuild", "-i", "test-instance"]);

        exitCode.Should().Be(0);

        // Verify cache has corrected mapping
        var cacheFile = GetCacheFilePath("radarr");
        var cacheContent = await Fs.File.ReadAllTextAsync(cacheFile);
        var cache = JsonSerializer.Deserialize<CustomFormatCacheObject>(
            cacheContent,
            GlobalJsonSerializerSettings.Recyclarr
        );

        cache.Should().NotBeNull();
        cache
            .Mappings.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new { TrashId = "trash-id-1", ServiceId = 20 });
    }

    [Test]
    public async Task Rebuild_preserves_cache_entries_that_are_already_correct()
    {
        SetupRadarrConfig("test-instance", "trash-id-1");
        SetupGuideCfs("radarr", ("trash-id-1", "Test CF"));
        SetupServiceCfs(new CustomFormatResource { Id = 10, Name = "Test CF" });

        // Existing cache already has correct ID
        var existingCache = new CustomFormatCacheObject
        {
            Mappings = [new TrashIdMapping("trash-id-1", "Test CF", ServiceId: 10)],
        };
        SetupExistingCache("radarr", existingCache);

        var exitCode = await CliSetup.Run(Container, ["cache", "rebuild", "-i", "test-instance"]);

        exitCode.Should().Be(0);

        // Verify cache preserved
        var cacheFile = GetCacheFilePath("radarr");
        var cacheContent = await Fs.File.ReadAllTextAsync(cacheFile);
        var cache = JsonSerializer.Deserialize<CustomFormatCacheObject>(
            cacheContent,
            GlobalJsonSerializerSettings.Recyclarr
        );

        cache.Should().NotBeNull();
        cache
            .Mappings.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new { TrashId = "trash-id-1", ServiceId = 10 });
    }

    [Test]
    public async Task Rebuild_without_adopt_skips_uncached_service_matches()
    {
        SetupRadarrConfig("test-instance", "trash-id-1");
        SetupGuideCfs("radarr", ("trash-id-1", "Test CF"));
        SetupServiceCfs(new CustomFormatResource { Id = 10, Name = "Test CF" });
        // No existing cache - service CF matches by name but isn't tracked

        var exitCode = await CliSetup.Run(Container, ["cache", "rebuild", "-i", "test-instance"]);

        exitCode.Should().Be(0);

        // Without --adopt, no cache file should be created (nothing to save)
        var cacheFile = GetCacheFilePath("radarr");
        cacheFile.Should().BeEmpty();
    }

    [Test]
    public async Task Rebuild_with_adopt_adds_uncached_service_matches()
    {
        SetupRadarrConfig("test-instance", "trash-id-1");
        SetupGuideCfs("radarr", ("trash-id-1", "Test CF"));
        SetupServiceCfs(new CustomFormatResource { Id = 10, Name = "Test CF" });
        // No existing cache - service CF matches by name but isn't tracked

        var exitCode = await CliSetup.Run(
            Container,
            ["cache", "rebuild", "-i", "test-instance", "--adopt"]
        );

        exitCode.Should().Be(0);

        // With --adopt, cache file should be created with the adopted mapping
        var cacheFile = GetCacheFilePath("radarr");
        Fs.File.Exists(cacheFile).Should().BeTrue();

        var cacheContent = await Fs.File.ReadAllTextAsync(cacheFile);
        var cache = JsonSerializer.Deserialize<CustomFormatCacheObject>(
            cacheContent,
            GlobalJsonSerializerSettings.Recyclarr
        );

        cache.Should().NotBeNull();
        cache
            .Mappings.Should()
            .ContainSingle()
            .Which.Should()
            .BeEquivalentTo(new { TrashId = "trash-id-1", ServiceId = 10 });
    }

    [Test]
    public async Task Rebuild_loads_cache_with_legacy_field_names()
    {
        SetupRadarrConfig("test-instance", "trash-id-1");
        SetupGuideCfs("radarr", ("trash-id-1", "Test CF"));
        SetupServiceCfs(new CustomFormatResource { Id = 10, Name = "Test CF" });

        // Legacy cache format (pre-v8.0) with old field names
        const string legacyCache = """
            {
              "version": 1,
              "trash_id_mappings": [{
                "trash_id": "trash-id-1",
                "custom_format_name": "Test CF",
                "custom_format_id": 10
              }]
            }
            """;
        SetupExistingCacheRaw("radarr", legacyCache);

        var exitCode = await CliSetup.Run(Container, ["cache", "rebuild", "-i", "test-instance"]);

        exitCode.Should().Be(0);

        // Cache should be unchanged (already correct)
        var cacheFile = GetCacheFilePath("radarr");
        var cacheContent = await Fs.File.ReadAllTextAsync(cacheFile);
        var cache = JsonSerializer.Deserialize<CustomFormatCacheObject>(
            cacheContent,
            GlobalJsonSerializerSettings.Recyclarr
        );

        cache!.Mappings.Should().BeEquivalentTo([new { TrashId = "trash-id-1", ServiceId = 10 }]);
    }

    private void SetupExistingCache(string serviceType, CustomFormatCacheObject cacheObj)
    {
        var cacheJson = JsonSerializer.Serialize(cacheObj, GlobalJsonSerializerSettings.Recyclarr);
        SetupExistingCacheRaw(serviceType, cacheJson);
    }

    private void SetupExistingCacheRaw(string serviceType, string cacheJson)
    {
        var cacheDir = Paths
            .CacheDirectory.SubDirectory(serviceType.ToLowerInvariant())
            .SubDirectory("8247e13ec45dc17b"); // Hash from test config

        Fs.Directory.CreateDirectory(cacheDir.FullName);
        Fs.AddFile(cacheDir.File("custom-format-cache.json"), new MockFileData(cacheJson));
    }

    private string GetCacheFilePath(string serviceType)
    {
        // Cache path is: {appdata}/cache/{service}/{hash}/custom-format-cache.json
        var cacheDir = Paths.CacheDirectory.SubDirectory(serviceType.ToLowerInvariant());

        if (!Fs.Directory.Exists(cacheDir.FullName))
        {
            return string.Empty;
        }

        var subdirs = Fs.Directory.GetDirectories(cacheDir.FullName);
        if (subdirs.Length == 0)
        {
            return string.Empty;
        }

        return Fs.Path.Combine(subdirs[0], "custom-format-cache.json");
    }
}
