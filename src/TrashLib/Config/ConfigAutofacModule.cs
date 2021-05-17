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
            builder.RegisterType<ServerInfo>().As<IServerInfo>();

            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AsClosedTypesOf(typeof(IValidator<>))
                .AsImplementedInterfaces();
        }
    }
}
