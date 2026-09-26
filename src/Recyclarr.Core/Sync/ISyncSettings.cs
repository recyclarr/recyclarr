using Recyclarr.TrashGuide;

namespace Recyclarr.Sync;

public interface ISyncSettings
{
    SupportedServices? Service { get; }
    bool Preview { get; }
    IReadOnlyCollection<string>? Instances { get; }
}
