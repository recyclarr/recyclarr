using Autofac;
using Recyclarr.ServarrApi.Http;
using Recyclarr.ServarrApi.Services;
using IFlurlClientFactory = Flurl.Http.Configuration.IFlurlClientFactory;

namespace Recyclarr.ServarrApi;

public class ApiServicesAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<SystemApiService>().As<ISystemApiService>();
        builder.RegisterType<FlurlClientFactory>().As<IFlurlClientFactory>().SingleInstance();
        builder.RegisterType<ServiceRequestBuilder>().As<IServiceRequestBuilder>();
        builder.RegisterType<QualityProfileService>().As<IQualityProfileService>();
        builder.RegisterType<CustomFormatService>().As<ICustomFormatService>();
        builder.RegisterType<QualityDefinitionService>().As<IQualityDefinitionService>();
    }
}
