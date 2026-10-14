using Autofac;

namespace Recyclarr.Compatibility;

public static class DetectedServiceScopeExtensions
{
    extension(ILifetimeScope instanceScope)
    {
        // Untagged child of the instance scope: per-instance ("instance"-matching) registrations
        // still resolve from the parent, while version-selected gateways resolve here.
        public ILifetimeScope BeginDetectedServiceScope(DetectedService service)
        {
            return instanceScope.BeginLifetimeScope(c => c.RegisterInstance(service));
        }
    }
}
