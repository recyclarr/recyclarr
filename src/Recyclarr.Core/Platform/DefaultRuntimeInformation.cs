using System.Runtime.InteropServices;

namespace Recyclarr.Platform;

public class DefaultRuntimeInformation : IRuntimeInformation
{
    public bool IsPlatformOsx()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }
}
