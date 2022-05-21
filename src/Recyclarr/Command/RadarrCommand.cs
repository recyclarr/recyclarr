using CliFx.Attributes;
using JetBrains.Annotations;
using Recyclarr.Command.Initialization;
using Recyclarr.Command.Services;

namespace Recyclarr.Command;

[Command("radarr", Description = "Perform operations on a Radarr instance")]
[UsedImplicitly]
internal class RadarrCommand : ServiceCommand, IRadarrCommand
{
    private readonly Lazy<RadarrService> _service;

    public override string CacheStoragePath { get; } =
        Path.Combine(AppPaths.AppDataPath, "cache", "radarr");

    public override string Name => "Radarr";

    public RadarrCommand(IServiceInitializationAndCleanup init, Lazy<RadarrService> service)
        : base(init)
    {
        _service = service;
    }

    protected override async Task Process()
    {
        await _service.Value.Execute(this);
    }
}
