using Autofac;
using Recyclarr.Config;

namespace Recyclarr.Tests.TestLibrary;

public class TestConfigurationScope : ConfigurationScope
{
    private ILifetimeScope? _lifetimeScope;

    public T Resolve<T>() where T : notnull
    {
        ArgumentNullException.ThrowIfNull(_lifetimeScope);
        return _lifetimeScope.Resolve<T>();
    }

    public override void SetScope(IDisposable scope)
    {
        base.SetScope(scope);
        _lifetimeScope = (ILifetimeScope) scope;
    }
}
