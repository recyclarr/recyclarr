using Recyclarr.Migration.Steps;
using Recyclarr.Platform;

namespace Recyclarr.Core.Tests.Migration.Steps;

internal sealed class NormalizeStateMappingsPropertyStepTest
{
    private readonly MockFileSystem _fs = new();
    private readonly ILogger _log = Substitute.For<ILogger>();

    private NormalizeStateMappingsPropertyStep CreateSut()
    {
        var appPaths = Substitute.For<IAppPaths>();
        appPaths.ConfigDirectory.Returns(_fs.DirectoryInfo.New("/app"));
        appPaths.StateDirectory.Returns(_fs.DirectoryInfo.New("/app/state"));
        return new NormalizeStateMappingsPropertyStep(appPaths);
    }

    [Test]
    public void Run_does_nothing_when_no_state_directory()
    {
        var sut = CreateSut();

        var act = () => sut.Run(_log);

        act.Should().NotThrow();
        _fs.Directory.Exists("/app/state").Should().BeFalse();
    }

    [Test]
    public void Execute_renames_trash_id_mappings_to_mappings()
    {
        const string original = """
            {
              "version": 1,
              "trash_id_mappings": [
                { "trash_id": "abc", "name": "Test", "service_id": 10 }
              ]
            }
            """;

        _fs.AddFile(
            "/app/state/radarr/abc123/custom-format-mappings.json",
            new MockFileData(original)
        );

        var sut = CreateSut();
        sut.Run(_log);

        var result = _fs.File.ReadAllText("/app/state/radarr/abc123/custom-format-mappings.json");

        result.Should().Contain("\"mappings\"");
        result.Should().NotContain("\"trash_id_mappings\"");
        result.Should().NotContain("\"version\"");
    }

    [Test]
    public void Execute_renames_TrashIdMappings_to_mappings()
    {
        const string original = """
            {
              "TrashIdMappings": [
                { "trash_id": "qp1", "name": "HD", "service_id": 5 }
              ]
            }
            """;

        _fs.AddFile(
            "/app/state/sonarr/def456/quality-profile-mappings.json",
            new MockFileData(original)
        );

        var sut = CreateSut();
        sut.Run(_log);

        var result = _fs.File.ReadAllText("/app/state/sonarr/def456/quality-profile-mappings.json");

        result.Should().Contain("\"mappings\"");
        result.Should().NotContain("\"TrashIdMappings\"");
    }

    [Test]
    public void Execute_preserves_mapping_data()
    {
        const string original = """
            {
              "trash_id_mappings": [
                { "trash_id": "id1", "name": "CF One", "service_id": 10 },
                { "trash_id": "id2", "name": "CF Two", "service_id": 20 }
              ]
            }
            """;

        _fs.AddFile(
            "/app/state/radarr/abc123/custom-format-mappings.json",
            new MockFileData(original)
        );

        var sut = CreateSut();
        sut.Run(_log);

        var result = _fs.File.ReadAllText("/app/state/radarr/abc123/custom-format-mappings.json");

        result.Should().Contain("id1");
        result.Should().Contain("CF One");
        result.Should().Contain("10");
        result.Should().Contain("id2");
        result.Should().Contain("CF Two");
        result.Should().Contain("20");
    }

    [Test]
    public void Execute_skips_already_canonical_files()
    {
        const string canonical = """{ "mappings": [] }""";

        _fs.AddFile(
            "/app/state/radarr/abc123/custom-format-mappings.json",
            new MockFileData(canonical)
        );

        var sut = CreateSut();
        sut.Run(_log);

        var result = _fs.File.ReadAllText("/app/state/radarr/abc123/custom-format-mappings.json");

        // File should be unchanged
        result.Should().Contain("\"mappings\"");
    }

    [Test]
    public void Execute_handles_multiple_services_and_hashes()
    {
        _fs.AddFile(
            "/app/state/radarr/hash1/custom-format-mappings.json",
            new MockFileData("""{ "trash_id_mappings": [] }""")
        );
        _fs.AddFile(
            "/app/state/sonarr/hash2/quality-profile-mappings.json",
            new MockFileData("""{ "TrashIdMappings": [] }""")
        );

        var sut = CreateSut();
        sut.Run(_log);

        _fs.File.ReadAllText("/app/state/radarr/hash1/custom-format-mappings.json")
            .Should()
            .Contain("\"mappings\"");
        _fs.File.ReadAllText("/app/state/sonarr/hash2/quality-profile-mappings.json")
            .Should()
            .Contain("\"mappings\"");
    }

    [Test]
    public void Run_leaves_malformed_json_unchanged()
    {
        const string path = "/app/state/radarr/abc123/custom-format-mappings.json";
        _fs.AddFile(path, new MockFileData("not valid json {{{"));

        var sut = CreateSut();
        sut.Run(_log);

        _fs.File.ReadAllText(path).Should().Be("not valid json {{{");
    }
}
