using Recyclarr.Sync;
using Recyclarr.TrashGuide;

namespace Recyclarr.Server.Sync;

internal sealed record ServerSyncSettings(
    SupportedServices? Service,
    IReadOnlyCollection<string> Instances,
    bool Preview,
    IReadOnlyCollection<string> Configs
) : ISyncSettings;
