using System.Reflection;
using Autofac;
using FluentValidation;
using TrashLib.Config.Services;
using Module = Autofac.Module;

namespace TrashLib.Config;

public class ConfigAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<ConfigurationProvider>()
            .As<IConfigurationProvider>()
            .SingleInstance();

        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .AsClosedTypesOf(typeof(IValidator<>))
            .AsImplementedInterfaces();
    }
}
