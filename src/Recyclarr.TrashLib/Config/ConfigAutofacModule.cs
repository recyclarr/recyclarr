using System.Reflection;
using Autofac;
using FluentValidation;
using Recyclarr.TrashLib.Config.Listers;
using Recyclarr.TrashLib.Config.Parsing;
using Recyclarr.TrashLib.Config.Secrets;
using Recyclarr.TrashLib.Config.Services;
using Recyclarr.TrashLib.Config.Settings;
using Recyclarr.TrashLib.Config.Yaml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;
using Module = Autofac.Module;

namespace Recyclarr.TrashLib.Config;

public class ConfigAutofacModule : Module
{
    private readonly Assembly[] _assemblies;

    public ConfigAutofacModule(params Assembly[] assemblies)
    {
        _assemblies = assemblies;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(_assemblies)
            .AsClosedTypesOf(typeof(IValidator<>))
            .As<IValidator>();

        builder.RegisterAssemblyTypes(_assemblies)
            .AssignableTo<IYamlBehavior>()
            .As<IYamlBehavior>();

        builder.RegisterType<SettingsProvider>().As<ISettingsProvider>().SingleInstance();
        builder.RegisterType<SecretsProvider>().As<ISecretsProvider>().SingleInstance();
        builder.RegisterType<YamlSerializerFactory>().As<IYamlSerializerFactory>();

        builder.RegisterType<DefaultObjectFactory>().As<IObjectFactory>();
        builder.RegisterType<ConfigurationLoader>().As<IConfigurationLoader>();
        builder.RegisterType<ConfigurationFinder>().As<IConfigurationFinder>();
        builder.RegisterType<ConfigTemplateGuideService>().As<IConfigTemplateGuideService>();
        builder.RegisterType<ConfigValidationExecutor>();
        builder.RegisterType<ConfigParser>();
        builder.RegisterType<BackwardCompatibleConfigParser>();

        // Config Listers
        builder.RegisterType<ConfigTemplateLister>().Keyed<IConfigLister>(ConfigListCategory.Templates);
        builder.RegisterType<ConfigLocalLister>().Keyed<IConfigLister>(ConfigListCategory.Local);
    }
}
