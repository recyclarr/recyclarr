using Autofac;
using Flurl.Http.Configuration;
using Recyclarr.ServarrApi.CustomFormat;
using Recyclarr.ServarrApi.Http;
using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.ServarrApi.QualityDefinition;
using Recyclarr.ServarrApi.QualityProfile;
using Recyclarr.ServarrApi.System;

namespace Recyclarr.ServarrApi;

public class ApiServicesAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);
        builder.RegisterType<FlurlClientFactory>().As<IFlurlClientFactory>().SingleInstance();

        builder.RegisterType<SystemApiService>().As<ISystemApiService>();
        builder.RegisterType<ServiceRequestBuilder>().As<IServiceRequestBuilder>();
        builder.RegisterType<QualityProfileApiService>().As<IQualityProfileApiService>();
        builder.RegisterType<CustomFormatApiService>().As<ICustomFormatApiService>();
        builder.RegisterType<QualityDefinitionApiService>().As<IQualityDefinitionApiService>();
        builder.RegisterType<MediaNamingApiService>().As<IMediaNamingApiService>();
    }
}
