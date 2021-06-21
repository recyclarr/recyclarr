using Autofac;
using TrashLib.Config;

namespace TrashLib.Cache
{
    public class CacheAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<ConfigAutofacModule>();

            builder.RegisterGeneric(typeof(CacheGuidBuilder<>))
                .As<ICacheGuidBuilder>();

            // Clients must register their own implementation of ICacheStoragePath
            builder.RegisterType<ServiceCache>().As<IServiceCache>();
        }
    }
}
