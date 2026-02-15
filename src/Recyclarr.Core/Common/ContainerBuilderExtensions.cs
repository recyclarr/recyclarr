using Autofac;
using Autofac.Builder;

namespace Recyclarr.Common;

public static class ContainerBuilderExtensions
{
    public static void RegisterMatchingScope(
        this ContainerBuilder builder,
        object tag,
        Action<ScopedRegistrationBuilder> configure
    )
    {
        var scoped = new ScopedRegistrationBuilder(builder, tag);
        configure(scoped);
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
