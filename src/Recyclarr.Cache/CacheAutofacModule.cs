using Autofac;

namespace Recyclarr.Cache;

public class CacheAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<CacheStoragePath>().As<ICacheStoragePath>();
        // builder.RegisterGeneric(typeof(CachePersister<>)).As(typeof(ICachePersister<>));
    }
}
