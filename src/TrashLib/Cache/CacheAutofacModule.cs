using Autofac;
using TrashLib.Config;

namespace TrashLib.Cache
{
    public class CacheAutofacModule : Module
    {
        // Clients must register their own implementation of ICacheStoragePath
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<ConfigAutofacModule>();

            builder.RegisterType<CacheGuidBuilder>().As<ICacheGuidBuilder>();
            builder.RegisterType<FilesystemServiceCache>().As<IServiceCache>();
        }
    }
}
