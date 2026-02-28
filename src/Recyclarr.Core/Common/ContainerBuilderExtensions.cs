using Autofac;
using Autofac.Builder;
using Recyclarr.Config.Models;
using Recyclarr.TrashGuide;

namespace Recyclarr.Common;

public static class ContainerBuilderExtensions
{
    extension(ContainerBuilder builder)
    {
        public void RegisterMatchingScope(object tag, Action<ScopedRegistrationBuilder> configure)
        {
            var scoped = new ScopedRegistrationBuilder(builder, tag);
            configure(scoped);
        }

        // Keyed adapter registration: registers one adapter per service (Sonarr, Radarr) and a
        // non-keyed resolution that selects the correct adapter based on IServiceConfiguration.
        public void RegisterServiceGateway<TPort, TSonarr, TRadarr>()
            where TPort : notnull
            where TSonarr : TPort
            where TRadarr : TPort
        {
            builder
                .RegisterType<TSonarr>()
                .Keyed<TPort>(SupportedServices.Sonarr)
                .InstancePerLifetimeScope();
            builder
                .RegisterType<TRadarr>()
                .Keyed<TPort>(SupportedServices.Radarr)
                .InstancePerLifetimeScope();
            builder
                .Register(c =>
                    c.ResolveKeyed<TPort>(c.Resolve<IServiceConfiguration>().ServiceType)
                )
                .As<TPort>()
                .InstancePerLifetimeScope();
        }
    }
}

public class ScopedRegistrationBuilder(ContainerBuilder builder, object tag)
{
    public IRegistrationBuilder<
        T,
        ConcreteReflectionActivatorData,
        SingleRegistrationStyle
    > RegisterType<T>()
        where T : notnull
    {
        return builder.RegisterType<T>().InstancePerMatchingLifetimeScope(tag);
    }
}
