using Autofac;

namespace Recyclarr.TrashLib.Services.System;

public class SystemServiceAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<SystemApiService>().As<ISystemApiService>();
        builder.RegisterType<ServiceInformation>().As<IServiceInformation>()
            .InstancePerLifetimeScope();
    }
}
