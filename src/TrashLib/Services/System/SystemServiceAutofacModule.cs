using Autofac;

namespace TrashLib.Services.System;

public class SystemServiceAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<SystemApiService>().As<ISystemApiService>();
    }
}
