using System.Globalization;
using System.IO.Abstractions;
using Recyclarr.Migration.Steps;
using Recyclarr.Platform;
using Serilog.Events;

namespace Recyclarr.Core.Tests.Migration.Steps;

internal sealed class FlattenNestedStateMigrationStepTest
{
    private readonly MockFileSystem _fs = new();
    private readonly RecordingLogger _log = new();

    // A v8 all-in-one volume mounted where v9 expects only state.
    private IDirectoryInfo State => _fs.CurrentDirectory().SubDirectory("state-volume");

    private FlattenNestedStateMigrationStep CreateSut(
        IDirectoryInfo? config = null,
        IDirectoryInfo? data = null
    )
    {
        var root = _fs.CurrentDirectory();
        var paths = new AppPaths(
            config ?? root.SubDirectory("config"),
            data ?? root.SubDirectory("data"),
            State
        );
        return new FlattenNestedStateMigrationStep(paths);
    }

    [Test]
    public void Moves_nested_state_to_the_state_root()
    {
        _fs.AddFile(
            State.File("state", "sonarr", "abc", "cf-mappings.json"),
            new MockFileData("1")
        );
        _fs.AddFile(
            State.File("state", "radarr", "def", "cf-mappings.json"),
            new MockFileData("2")
        );

        CreateSut().Run(_log);

        _fs.GetFile(State.File("sonarr", "abc", "cf-mappings.json")).TextContents.Should().Be("1");
        _fs.GetFile(State.File("radarr", "def", "cf-mappings.json")).TextContents.Should().Be("2");
        State.SubDirectory("state").Exists.Should().BeFalse();
    }

    [Test]
    public void Reports_leftover_top_level_entries_without_deleting_them()
    {
        _fs.AddFile(State.File("state", "sonarr", "abc", "cf-mappings.json"), new MockFileData(""));
        _fs.AddFile(State.File("configs", "a.yml"), new MockFileData(""));
        _fs.AddFile(State.File("configs", "b.yml"), new MockFileData(""));
        _fs.AddFile(State.File("settings.yml"), new MockFileData(""));
        _fs.AddFile(State.File("recyclarr-server.lock"), new MockFileData(""));

        CreateSut().Run(_log);

        State.File("configs", "a.yml").Exists.Should().BeTrue();
        State.File("settings.yml").Exists.Should().BeTrue();
        _log.Events.Should()
            .ContainSingle(e => e.Level == LogEventLevel.Warning)
            .Which.RenderMessage(CultureInfo.InvariantCulture)
            .Should()
            .Contain("configs")
            .And.Contain("settings.yml")
            .And.NotContain("a.yml")
            .And.NotContain("recyclarr-server.lock")
            .And.NotContain("sonarr");
    }

    [Test]
    public void Does_nothing_without_nested_state()
    {
        _fs.AddFile(State.File("sonarr", "abc", "cf-mappings.json"), new MockFileData(""));

        CreateSut().Run(_log);

        State.File("sonarr", "abc", "cf-mappings.json").Exists.Should().BeTrue();
        _log.Events.Should().BeEmpty();
    }

    [Test]
    public void Does_nothing_when_the_root_already_has_state()
    {
        _fs.AddFile(State.File("state", "sonarr", "abc", "old.json"), new MockFileData(""));
        _fs.AddFile(State.File("radarr", "def", "new.json"), new MockFileData(""));

        CreateSut().Run(_log);

        State.File("state", "sonarr", "abc", "old.json").Exists.Should().BeTrue();
        State.SubDirectory("sonarr").Exists.Should().BeFalse();
        _log.Events.Should().BeEmpty();
    }

    [Test]
    public void Does_nothing_when_state_is_the_config_directory()
    {
        _fs.AddFile(State.File("state", "sonarr", "abc", "cf.json"), new MockFileData(""));

        CreateSut(config: State).Run(_log);

        State.File("state", "sonarr", "abc", "cf.json").Exists.Should().BeTrue();
        _log.Events.Should().BeEmpty();
    }

    [Test]
    public void Does_nothing_when_state_is_the_data_directory()
    {
        _fs.AddFile(State.File("state", "sonarr", "abc", "cf.json"), new MockFileData(""));

        CreateSut(data: State).Run(_log);

        State.File("state", "sonarr", "abc", "cf.json").Exists.Should().BeTrue();
        _log.Events.Should().BeEmpty();
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<LogEvent> Events { get; } = [];

        public void Write(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }
    }
}
