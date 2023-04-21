using Recyclarr.TrashLib.Config;
using Recyclarr.TrashLib.Config.Services;

namespace Recyclarr.TrashLib.TestLibrary;

public class TestConfig : ServiceConfiguration
{
    public override SupportedServices ServiceType => SupportedServices.Sonarr;
}
