using Autofac;

namespace Recyclarr.Cli.Cache;

public class CacheAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<CacheStoragePath>().As<ICacheStoragePath>();
        builder.RegisterType<ServiceCache>().As<IServiceCache>();
    }
}
