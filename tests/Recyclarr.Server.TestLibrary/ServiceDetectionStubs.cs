using Autofac;
using NSubstitute;
using Recyclarr.Compatibility;
using Recyclarr.Compatibility.Radarr;

namespace Recyclarr.Server.TestLibrary;

public static class ServiceDetectionStubs
{
    extension(ContainerBuilder builder)
    {
        /// <summary>
        /// Lets calls to a configured Radarr instance pass service version detection without
        /// network access, so tests reach the gateway stubs they register.
        /// </summary>
        public void RegisterRadarrDetection()
        {
            var info = Substitute.For<IServiceInformation>();
            info.GetAppName(default).ReturnsForAnyArgs("Radarr");
            info.GetVersion(default).ReturnsForAnyArgs(new Version(6, 0));
            builder.RegisterInstance(info).As<IServiceInformation>();

            var capabilities = Substitute.For<IRadarrCapabilityFetcher>();
            capabilities
                .GetCapabilities(default)
                .ReturnsForAnyArgs(new RadarrCapabilities(new Version(6, 0)));
            builder.RegisterInstance(capabilities).As<IRadarrCapabilityFetcher>();
        }
    }
}
