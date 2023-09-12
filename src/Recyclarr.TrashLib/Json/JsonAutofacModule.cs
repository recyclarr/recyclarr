using Autofac;
using Newtonsoft.Json;

namespace Recyclarr.TrashLib.Json;

public class JsonAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.Register<Func<JsonSerializerSettings, IBulkJsonLoader>>(c =>
        {
            return settings => new BulkJsonLoader(c.Resolve<ILogger>(), settings);
        });

        // Decorators for BulkJsonLoader. We do not use RegisterDecorator() here for these reasons:
        // - We consume the BulkJsonLoader as a delegate factory, not by instance
        // - We do not want all implementations of BulkJsonLoader to be decorated, only a specific implementation.
        builder.RegisterType<GuideJsonLoader>();
        builder.RegisterType<ServiceJsonLoader>();
    }
}
