using System.IO.Abstractions;
using CliFx.Attributes;
using JetBrains.Annotations;
using Recyclarr.Command.Initialization;
using Recyclarr.Command.Services;
using TrashLib;

namespace Recyclarr.Command;

[Command("sonarr", Description = "Perform operations on a Sonarr instance")]
[UsedImplicitly]
public class SonarrCommand : ServiceCommand, ISonarrCommand
{
    private readonly Lazy<SonarrService> _service;
    private readonly string? _cacheStoragePath;

    [CommandOption("list-release-profiles", Description =
        "List available release profiles from the guide in YAML format.")]
    public bool ListReleaseProfiles { get; [UsedImplicitly] set; }

    // The default value is "empty" because I need to know when the user specifies the option but no value with it.
    // Discussed here: https://github.com/Tyrrrz/CliFx/discussions/128#discussioncomment-2647015
    [CommandOption("list-terms", Description =
        "For the given Release Profile Trash ID, list terms in it that can be filtered in YAML format. " +
        "Note that not every release profile has terms that may be filtered.")]
    public string? ListTerms { get; [UsedImplicitly] set; } = "empty";

    public sealed override string CacheStoragePath
    {
        get => _cacheStoragePath ?? _service.Value.DefaultCacheStoragePath;
        protected init => _cacheStoragePath = value;
    }

    public override string Name => "Sonarr";

    public SonarrCommand(
        IServiceInitializationAndCleanup init,
        Lazy<SonarrService> service,
        IFileSystem fs,
        IAppPaths paths)
        : base(init)
    {
        _service = service;
    }

    protected override async Task Process()
    {
        await _service.Value.Execute(this);
    }
}
