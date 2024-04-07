using Autofac;
using Flurl.Http.Configuration;

namespace Recyclarr.Http;

public class HttpAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<FlurlClientCache>()
            .As<IFlurlClientCache>()
            .SingleInstance();

        builder.RegisterTypes(
                typeof(FlurlAfterCallLogRedactor),
                typeof(FlurlBeforeCallLogRedactor),
                typeof(FlurlRedirectPreventer))
            .As<FlurlSpecificEventHandler>();
    }
}
