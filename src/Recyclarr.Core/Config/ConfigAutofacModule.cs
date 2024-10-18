using Autofac;
using Autofac.Extras.Ordering;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using FluentValidation;
using Recyclarr.Config.Parsing;
using Recyclarr.Config.Parsing.PostProcessing;
using Recyclarr.Config.Parsing.PostProcessing.ConfigMerging;
using Recyclarr.Config.Parsing.PostProcessing.Deprecations;
using Recyclarr.Config.Secrets;
using Recyclarr.Yaml;

namespace Recyclarr.Config;

public class ConfigAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAutoMapper(ThisAssembly);

        builder.RegisterAssemblyTypes(ThisAssembly)
            .AssignableTo<IYamlBehavior>()
            .As<IYamlBehavior>();

        builder.RegisterType<SecretsProvider>().As<ISecretsProvider>().SingleInstance();
        builder.RegisterType<YamlIncludeResolver>().As<IYamlIncludeResolver>();
        builder.RegisterType<ConfigurationRegistry>().As<IConfigurationRegistry>();
        builder.RegisterType<ConfigurationLoader>().As<IConfigurationLoader>();
        builder.RegisterType<ConfigurationFinder>().As<IConfigurationFinder>();
        builder.RegisterType<ConfigValidationExecutor>();
        builder.RegisterType<ConfigParser>();
        builder.RegisterType<ConfigSaver>();
        builder.RegisterType<ConfigurationScopeFactory>();

        // Keyed include processors
        builder.RegisterType<ConfigIncludeProcessor>().Keyed<IIncludeProcessor>(typeof(ConfigYamlInclude));
        builder.RegisterType<TemplateIncludeProcessor>().Keyed<IIncludeProcessor>(typeof(TemplateYamlInclude));

        // Config Post Processors
        builder.RegisterTypes(
                // Order-sensitive!
                typeof(ConfigDeprecationPostProcessor),
                typeof(ImplicitUrlAndKeyPostProcessor),
                typeof(IncludePostProcessor))
            .As<IConfigPostProcessor>()
            .OrderByRegistration();

        // Config Deprecations
        builder.RegisterType<ConfigDeprecations>();
        builder.RegisterTypes(
                // Order-sensitive!
                typeof(CfQualityProfilesDeprecationCheck))
            .As<IConfigDeprecationCheck>()
            .OrderByRegistration();

        RegisterValidators(builder);
    }

    private static void RegisterValidators(ContainerBuilder builder)
    {
        builder.RegisterType<RootConfigYamlValidator>().As<IValidator>();

        // These validators are required by IncludePostProcessor
        builder.RegisterType<RadarrConfigYamlValidator>().As<IValidator>();
        builder.RegisterType<SonarrConfigYamlValidator>().As<IValidator>();
    }
}
