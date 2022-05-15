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
    private readonly string? _cacheStoragePath;

    public override string Name => "Radarr";

    public sealed override string CacheStoragePath
    {
        get => _cacheStoragePath ?? _service.Value.DefaultCacheStoragePath;
        protected init => _cacheStoragePath = value;
    }

    public RadarrCommand(
        IServiceInitializationAndCleanup init,
        Lazy<RadarrService> service)
        : base(init)
    {
        _service = service;
    }

    protected override async Task Process()
    {
        await _service.Value.Execute(this);
    }
}
