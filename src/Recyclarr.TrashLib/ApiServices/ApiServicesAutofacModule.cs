using Autofac;
using Recyclarr.TrashLib.ApiServices.System;

namespace Recyclarr.TrashLib.ApiServices;

public class ApiServicesAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<SystemApiService>().As<ISystemApiService>();
        builder.RegisterType<ServiceInformation>().As<IServiceInformation>()
            .InstancePerLifetimeScope();
    }
}
