using System.Reflection;
using Autofac;
using FluentValidation;
using Recyclarr.TrashLib.Config.Secrets;
using Recyclarr.TrashLib.Config.Settings;
using Recyclarr.TrashLib.Config.Yaml;
using Module = Autofac.Module;

namespace Recyclarr.TrashLib.Config;

public class ConfigAutofacModule : Module
{
    private readonly Assembly[] _assemblies;

    public ConfigAutofacModule(Assembly[] assemblies)
    {
        _assemblies = assemblies;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(_assemblies)
            .AsClosedTypesOf(typeof(IValidator<>))
            .AsImplementedInterfaces();

        builder.RegisterAssemblyTypes(_assemblies)
            .AssignableTo<IYamlBehavior>()
            .As<IYamlBehavior>();

        builder.RegisterType<SettingsProvider>().As<ISettingsProvider>().SingleInstance();
        builder.RegisterType<SecretsProvider>().As<ISecretsProvider>().SingleInstance();
        builder.RegisterType<YamlSerializerFactory>().As<IYamlSerializerFactory>();
    }
}
