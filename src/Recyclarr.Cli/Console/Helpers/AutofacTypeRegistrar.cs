using Autofac;
using Spectre.Console.Cli;

namespace Recyclarr.Cli.Console.Helpers;

internal class AutofacTypeRegistrar(ContainerBuilder builder)
    : ITypeRegistrar
{
    public void Register(Type service, Type implementation)
    {
        builder.RegisterType(implementation).As(service).SingleInstance();
    }

    public void RegisterInstance(Type service, object implementation)
    {
        builder.RegisterInstance(implementation).As(service);
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        builder.Register(_ => factory()).As(service).SingleInstance();
    }

    public ITypeResolver Build()
    {
        return new AutofacTypeResolver(builder.Build());
    }
}
