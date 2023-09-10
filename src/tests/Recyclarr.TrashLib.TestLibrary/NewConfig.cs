using Recyclarr.TrashLib.Config;

namespace Recyclarr.TrashLib.TestLibrary;

public static class NewConfig
{
    public static RadarrConfiguration Radarr()
    {
        return new RadarrConfiguration
        {
            InstanceName = ""
        };
    }

    public static SonarrConfiguration Sonarr()
    {
        return new SonarrConfiguration
        {
            InstanceName = ""
        };
    }
}
