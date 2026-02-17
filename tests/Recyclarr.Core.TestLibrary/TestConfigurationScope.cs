using Autofac;
using Recyclarr.Config;

namespace Recyclarr.Core.TestLibrary;

public class TestConfigurationScope(ILifetimeScope scope) : LifetimeScopeWrapper(scope)
{
    public T Resolve<T>()
        where T : notnull
    {
        return Scope.Resolve<T>();
    }
}
