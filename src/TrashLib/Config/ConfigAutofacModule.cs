using System.Reflection;
using Autofac;
using FluentValidation;
using Module = Autofac.Module;

namespace TrashLib.Config
{
    public class ConfigAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterGeneric(typeof(GenericConfigProvider<>))
                .As(typeof(IConfigProvider<>))
                .InstancePerLifetimeScope();

            builder.RegisterGeneric(typeof(ServerInfo<>)).As(typeof(IServerInfo<>));

            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AsClosedTypesOf(typeof(IValidator<>))
                .AsImplementedInterfaces();
        }
    }
}
