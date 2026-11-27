using System.IO.Abstractions;
using Autofac;
using Recyclarr.Repo;
using Recyclarr.Server.Diagnostics;
using Recyclarr.Server.Features.Sync.GetResults;
using Recyclarr.Server.Resources;
using Recyclarr.Server.Tests.Resources;
using Recyclarr.Server.Tests.Reusable;
using Recyclarr.VersionControl;

namespace Recyclarr.Server.Tests.Diagnostics;

/// <summary>
/// Each diagnostic the resource coordinator owns appears when the server sees its condition and
/// leaves the list once the server sees the condition is gone (ADR-035). The diagnostics list is
/// the state the diagnostics endpoint and notifications read.
/// </summary>
internal sealed class ServerDiagnosticsLifecycleTest : ServerIntegrationFixture
{
    private const string Guides = "trash-guides";
    private const string Templates = "config-templates";

    private readonly TimerObservingTimeProvider _time = new();
    private FakeGitRemote _remote = null!;

    private ResourceCoordinator Coordinator => Resolve<ResourceCoordinator>();

    private IReadOnlyList<string> CurrentCodes =>
        Resolve<ServerDiagnostics>().Current.Select(x => x.Code).ToList();

    protected override void RegisterStubsAndMocks(ContainerBuilder builder)
    {
        base.RegisterStubsAndMocks(builder);
        builder.RegisterInstance(_time).As<TimeProvider>();
        builder.RegisterType<FakeGitRemote>().As<IRepoUpdater>().AsSelf().SingleInstance();
        builder.RegisterType<FakeGitRemote.Repository>().As<IGitRepository>();
    }

    [Before(Test)]
    public void SetUp()
    {
        Fs.AddFile(
            Paths.ConfigDirectory.File("settings.yml"),
            new MockFileData(
                """
                resource_providers:
                  - name: includes
                    type: config-templates
                    clone_url: https://example.com/includes.git
                """
            )
        );
        _remote = Resolve<FakeGitRemote>();
        _remote.Commit(
            Guides,
            new Dictionary<string, string>
            {
                ["metadata.json"] = """{ "json_paths": { "radarr": {}, "sonarr": {} } }""",
            }
        );
        CommitTemplates("movie-include");
    }

    private void CommitTemplates(string includeId) =>
        _remote.Commit(
            Templates,
            new Dictionary<string, string>
            {
                ["includes.json"] = $$"""
                { "radarr": [{ "id": "{{includeId}}", "template": "inc.yml" }], "sonarr": [] }
                """,
                ["inc.yml"] = "quality_definition:\n  type: movie\n",
            }
        );

    private void WriteConfig(string extra = "") =>
        Fs.AddFile(
            Paths.ConfigDirectory.File("recyclarr.yml"),
            new MockFileData(
                $"""
                radarr:
                  movies:
                    base_url: http://movies:7878
                    api_key: asdf
                    {extra}
                """
            )
        );

    private Task Startup() => Coordinator.RefreshAsync(RefreshMode.Startup, CancellationToken.None);

    private Task Background() =>
        Coordinator.RefreshAsync(RefreshMode.Background, CancellationToken.None);

    private Task GuideCheck()
    {
        _time.Advance(ResourceCoordinator.Ttl);
        return Background();
    }

    [Test]
    public async Task Deprecated_property_is_listed_until_an_edit_removes_it()
    {
        WriteConfig("replace_existing_custom_formats: true");
        await Startup();
        var listed = Resolve<ServerDiagnostics>().Current.Should().ContainSingle().Which;

        WriteConfig();
        await Background();

        listed.Code.Should().Be("configPropertyDeprecated");
        listed.Message.Should().StartWith("recyclarr.yml: ");
        CurrentCodes.Should().BeEmpty();
    }

    [Test]
    public async Task Invalid_edit_is_an_error_until_the_edit_is_corrected()
    {
        WriteConfig();
        await Startup();
        Fs.AddFile(Paths.ConfigDirectory.File("recyclarr.yml"), new MockFileData("radarr: ["));

        await Background();
        await Background();
        var listed = Resolve<ServerDiagnostics>().Current.Should().ContainSingle().Which;
        WriteConfig();
        await Background();

        listed.Code.Should().Be("configEditRejected");
        listed.Severity.Should().Be(OutcomeSeverity.Error);
        CurrentCodes.Should().BeEmpty();
    }

    [Test]
    public async Task Held_back_guide_update_is_listed_until_a_later_update_validates()
    {
        WriteConfig("include:\n      - template: movie-include");
        await Startup();
        CommitTemplates("renamed-include");

        await GuideCheck();
        var held = CurrentCodes;
        await GuideCheck();
        var stillHeld = CurrentCodes;
        CommitTemplates("movie-include");
        await GuideCheck();

        held.Should().Equal("guideUpdateRejected");
        stillHeld.Should().Equal("guideUpdateRejected");
        CurrentCodes.Should().BeEmpty();
    }

    [Test]
    public async Task Failed_guide_download_is_listed_until_a_download_succeeds()
    {
        WriteConfig();
        await Startup();
        _remote.Offline = true;

        await GuideCheck();
        var failed = CurrentCodes;
        _remote.Offline = false;
        await GuideCheck();

        failed.Should().OnlyContain(x => x == "resourceProviderUpdateFailed").And.NotBeEmpty();
        CurrentCodes.Should().BeEmpty();
    }
}
