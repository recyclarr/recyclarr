using Autofac;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace Recyclarr.Config;

public class ObjectFactory : IObjectFactory
{
    private readonly ILifetimeScope _container;
    private readonly DefaultObjectFactory _defaultFactory = new();

    public ObjectFactory(ILifetimeScope container)
    {
        _container = container;
    }

    public object Create(Type type)
    {
        return _container.IsRegistered(type) ? _container.Resolve(type) : _defaultFactory.Create(type);
    }
}
