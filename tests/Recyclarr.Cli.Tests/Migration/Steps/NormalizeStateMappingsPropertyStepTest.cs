using Recyclarr.Cli.Migration.Steps;
using Recyclarr.Platform;

namespace Recyclarr.Cli.Tests.Migration.Steps;

internal sealed class NormalizeStateMappingsPropertyStepTest
{
    private static NormalizeStateMappingsPropertyStep CreateSut(
        MockFileSystem fs,
        IAppPaths appPaths
    )
    {
        appPaths.ConfigDirectory.Returns(fs.DirectoryInfo.New("/app"));
        appPaths.StateDirectory.Returns(fs.DirectoryInfo.New("/app/state"));
        return new NormalizeStateMappingsPropertyStep(appPaths);
    }

    [Test, AutoMockData]
    public void CheckIfNeeded_returns_false_when_no_state_directory(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths
    )
    {
        var sut = CreateSut(fs, appPaths);

        sut.CheckIfNeeded().Should().BeFalse();
    }

    [Test, AutoMockData]
    public void CheckIfNeeded_returns_false_when_files_already_canonical(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths
    )
    {
        fs.AddFile(
            "/app/state/radarr/abc123/custom-format-mappings.json",
            new MockFileData("""{ "mappings": [] }""")
        );

        var sut = CreateSut(fs, appPaths);

        sut.CheckIfNeeded().Should().BeFalse();
    }

    [Test, AutoMockData]
    public void CheckIfNeeded_returns_true_for_cf_legacy_property(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths
    )
    {
        fs.AddFile(
            "/app/state/radarr/abc123/custom-format-mappings.json",
            new MockFileData("""{ "trash_id_mappings": [] }""")
        );

        var sut = CreateSut(fs, appPaths);

        sut.CheckIfNeeded().Should().BeTrue();
    }

    [Test, AutoMockData]
    public void CheckIfNeeded_returns_true_for_qp_legacy_property(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths
    )
    {
        fs.AddFile(
            "/app/state/sonarr/abc123/quality-profile-mappings.json",
            new MockFileData("""{ "TrashIdMappings": [] }""")
        );

        var sut = CreateSut(fs, appPaths);

        sut.CheckIfNeeded().Should().BeTrue();
    }

    [Test, AutoMockData]
    public void Execute_renames_trash_id_mappings_to_mappings(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        ILogger log
    )
    {
        const string original = """
            {
              "version": 1,
              "trash_id_mappings": [
                { "trash_id": "abc", "name": "Test", "service_id": 10 }
              ]
            }
            """;

        fs.AddFile(
            "/app/state/radarr/abc123/custom-format-mappings.json",
            new MockFileData(original)
        );

        var sut = CreateSut(fs, appPaths);
        sut.Execute(log);

        var result = fs.File.ReadAllText("/app/state/radarr/abc123/custom-format-mappings.json");

        result.Should().Contain("\"mappings\"");
        result.Should().NotContain("\"trash_id_mappings\"");
        result.Should().NotContain("\"version\"");
    }

    [Test, AutoMockData]
    public void Execute_renames_TrashIdMappings_to_mappings(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        ILogger log
    )
    {
        const string original = """
            {
              "TrashIdMappings": [
                { "trash_id": "qp1", "name": "HD", "service_id": 5 }
              ]
            }
            """;

        fs.AddFile(
            "/app/state/sonarr/def456/quality-profile-mappings.json",
            new MockFileData(original)
        );

        var sut = CreateSut(fs, appPaths);
        sut.Execute(log);

        var result = fs.File.ReadAllText("/app/state/sonarr/def456/quality-profile-mappings.json");

        result.Should().Contain("\"mappings\"");
        result.Should().NotContain("\"TrashIdMappings\"");
    }

    [Test, AutoMockData]
    public void Execute_preserves_mapping_data(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        ILogger log
    )
    {
        const string original = """
            {
              "trash_id_mappings": [
                { "trash_id": "id1", "name": "CF One", "service_id": 10 },
                { "trash_id": "id2", "name": "CF Two", "service_id": 20 }
              ]
            }
            """;

        fs.AddFile(
            "/app/state/radarr/abc123/custom-format-mappings.json",
            new MockFileData(original)
        );

        var sut = CreateSut(fs, appPaths);
        sut.Execute(log);

        var result = fs.File.ReadAllText("/app/state/radarr/abc123/custom-format-mappings.json");

        result.Should().Contain("id1");
        result.Should().Contain("CF One");
        result.Should().Contain("10");
        result.Should().Contain("id2");
        result.Should().Contain("CF Two");
        result.Should().Contain("20");
    }

    [Test, AutoMockData]
    public void Execute_skips_already_canonical_files(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        ILogger log
    )
    {
        const string canonical = """{ "mappings": [] }""";

        fs.AddFile(
            "/app/state/radarr/abc123/custom-format-mappings.json",
            new MockFileData(canonical)
        );

        var sut = CreateSut(fs, appPaths);
        sut.Execute(log);

        var result = fs.File.ReadAllText("/app/state/radarr/abc123/custom-format-mappings.json");

        // File should be unchanged
        result.Should().Contain("\"mappings\"");
    }

    [Test, AutoMockData]
    public void Execute_handles_multiple_services_and_hashes(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths,
        ILogger log
    )
    {
        fs.AddFile(
            "/app/state/radarr/hash1/custom-format-mappings.json",
            new MockFileData("""{ "trash_id_mappings": [] }""")
        );
        fs.AddFile(
            "/app/state/sonarr/hash2/quality-profile-mappings.json",
            new MockFileData("""{ "TrashIdMappings": [] }""")
        );

        var sut = CreateSut(fs, appPaths);
        sut.Execute(log);

        fs.File.ReadAllText("/app/state/radarr/hash1/custom-format-mappings.json")
            .Should()
            .Contain("\"mappings\"");
        fs.File.ReadAllText("/app/state/sonarr/hash2/quality-profile-mappings.json")
            .Should()
            .Contain("\"mappings\"");
    }

    [Test, AutoMockData]
    public void CheckIfNeeded_ignores_malformed_json(
        [Frozen(Matching.ImplementedInterfaces)] MockFileSystem fs,
        [Frozen] IAppPaths appPaths
    )
    {
        fs.AddFile(
            "/app/state/radarr/abc123/custom-format-mappings.json",
            new MockFileData("not valid json {{{")
        );

        var sut = CreateSut(fs, appPaths);

        sut.CheckIfNeeded().Should().BeFalse();
    }
}
