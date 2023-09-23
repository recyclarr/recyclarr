using Recyclarr.Cli.Console.Settings;
using Recyclarr.Common;

namespace Recyclarr.Cli.TestLibrary;

public static class MockSyncSettings
{
    private static ISyncSettings MakeSyncSettings(SupportedServices? service, params string[] instances)
    {
        var settings = Substitute.For<ISyncSettings>();
        settings.Service.Returns(service);
        settings.Instances.Returns(instances);
        return settings;
    }

    public static ISyncSettings Radarr(params string[] instances)
    {
        return MakeSyncSettings(SupportedServices.Radarr, instances);
    }

    public static ISyncSettings Sonarr(params string[] instances)
    {
        return MakeSyncSettings(SupportedServices.Sonarr, instances);
    }

    public static ISyncSettings AnyService(params string[] instances)
    {
        return MakeSyncSettings(null, instances);
    }
}
