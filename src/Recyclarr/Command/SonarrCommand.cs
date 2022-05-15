using CliFx.Attributes;
using JetBrains.Annotations;
using Recyclarr.Command.Services;
using Recyclarr.Migration;

namespace Recyclarr.Command;

[Command("sonarr", Description = "Perform operations on a Sonarr instance")]
[UsedImplicitly]
internal class SonarrCommand : ServiceCommand, ISonarrCommand
{
    private readonly Lazy<SonarrService> _service;

    [CommandOption("list-release-profiles", Description =
        "List available release profiles from the guide in YAML format.")]
    public bool ListReleaseProfiles { get; [UsedImplicitly] set; }

    // The default value is "empty" because I need to know when the user specifies the option but no value with it.
    // Discussed here: https://github.com/Tyrrrz/CliFx/discussions/128#discussioncomment-2647015
    [CommandOption("list-terms", Description =
        "For the given Release Profile Trash ID, list terms in it that can be filtered in YAML format. " +
        "Note that not every release profile has terms that may be filtered.")]
    public string? ListTerms { get; [UsedImplicitly] set; } = "empty";

    public override string CacheStoragePath { get; } =
        Path.Combine(AppPaths.AppDataPath, "cache", "sonarr");

    public override string Name => "Sonarr";

    public SonarrCommand(IMigrationExecutor migration, Lazy<SonarrService> service)
        : base(migration)
    {
        _service = service;
    }

    protected override async Task Process()
    {
        await _service.Value.Execute(this);
    }
}
