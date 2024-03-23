using Autofac;
using Flurl.Http.Configuration;
using Recyclarr.ServarrApi.CustomFormat;
using Recyclarr.ServarrApi.Http;
using Recyclarr.ServarrApi.Http.Servarr;
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
        builder.RegisterType<FlurlClientCache>()
            .As<IFlurlClientCache>()
            .SingleInstance();

        builder.RegisterType<SystemApiService>().As<ISystemApiService>();
        builder.RegisterType<ServarrRequestBuilder>().As<IServarrRequestBuilder>();
        builder.RegisterType<QualityProfileApiService>().As<IQualityProfileApiService>();
        builder.RegisterType<CustomFormatApiService>().As<ICustomFormatApiService>();
        builder.RegisterType<QualityDefinitionApiService>().As<IQualityDefinitionApiService>();
        builder.RegisterType<MediaNamingApiService>().As<IMediaNamingApiService>();

        builder.RegisterTypes(
                typeof(FlurlAfterCallHandler),
                typeof(FlurlBeforeCallHandler),
                typeof(FlurlRedirectHandler))
            .As<FlurlSpecificEventHandler>();
    }
}
