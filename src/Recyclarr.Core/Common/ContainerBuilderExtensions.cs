using Autofac;
using Autofac.Builder;
using Recyclarr.Compatibility;

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

        // Registers the port as a non-keyed service that selects the gateway keyed by the
        // detected API target. Resolving the port before detection fails because
        // DetectedService is not registered yet.
        public ServiceGatewayRegistration<TPort> RegisterServiceGateway<TPort>()
            where TPort : notnull
        {
            builder
                .Register(c => c.ResolveKeyed<TPort>(c.Resolve<DetectedService>().ApiTarget))
                .As<TPort>()
                .InstancePerLifetimeScope();

            return new ServiceGatewayRegistration<TPort>(builder);
        }
    }
}

public class ServiceGatewayRegistration<TPort>(ContainerBuilder builder)
    where TPort : notnull
{
    public ServiceGatewayRegistration<TPort> For<TGateway>(ServiceApiTarget target)
        where TGateway : TPort
    {
        builder.RegisterType<TGateway>().Keyed<TPort>(target).InstancePerLifetimeScope();
        return this;
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
