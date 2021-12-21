using System;

namespace TrashLib.Sonarr;

public interface ISonarrCompatibility
{
    IObservable<SonarrCapabilities> Capabilities { get; }
    Version MinimumVersion { get; }
}
