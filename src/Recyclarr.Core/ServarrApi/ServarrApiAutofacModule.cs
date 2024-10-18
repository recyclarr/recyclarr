using Autofac;
using Recyclarr.ServarrApi.CustomFormat;
using Recyclarr.ServarrApi.MediaNaming;
using Recyclarr.ServarrApi.QualityDefinition;
using Recyclarr.ServarrApi.QualityProfile;
using Recyclarr.ServarrApi.System;

namespace Recyclarr.ServarrApi;

public class ServarrApiAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        base.Load(builder);

        // This is used by all specific API service classes registered below.
        builder.RegisterType<ServarrRequestBuilder>().As<IServarrRequestBuilder>();

        builder.RegisterType<SystemApiService>().As<ISystemApiService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<QualityProfileApiService>().As<IQualityProfileApiService>()
            .InstancePerLifetimeScope();
        builder.RegisterType<CustomFormatApiService>().As<ICustomFormatApiService>()
            .InstancePerLifetimeScope();
        builder.RegisterType<QualityDefinitionApiService>().As<IQualityDefinitionApiService>()
            .InstancePerLifetimeScope();
        builder.RegisterType<MediaNamingApiService>().As<IMediaNamingApiService>()
            .InstancePerLifetimeScope();
    }
}
