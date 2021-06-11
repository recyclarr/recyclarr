using Autofac;

namespace TrashLib.Cache
{
    public class CacheAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Clients must register their own implementation of ICacheStoragePath
            builder.RegisterType<ServiceCache>().As<IServiceCache>();
        }
    }
}
