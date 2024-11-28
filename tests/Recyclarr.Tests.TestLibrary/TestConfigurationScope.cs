using Autofac;
using Recyclarr.Config;

namespace Recyclarr.Tests.TestLibrary;

public class TestConfigurationScope(ILifetimeScope scope) : ConfigurationScope(scope)
{
    public T Resolve<T>()
        where T : notnull
    {
        return Scope.Resolve<T>();
    }
}
