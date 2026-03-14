using Autofac;
using Recyclarr.Cli.Console.Wizard.ViewModels;
using Recyclarr.ResourceProviders.Domain;

namespace Recyclarr.Cli.Console.Wizard;

internal sealed class WizardAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<WizardApplication>();
        builder.RegisterType<WizardViewModel>().SingleInstance();

        // Step ViewModels registered in wizard flow order.
        // Autofac preserves registration order for IEnumerable<T>.
        builder.RegisterType<ServiceTypeViewModel>().As<IWizardStepViewModel>();
        builder.RegisterType<ConnectionViewModel>().As<IWizardStepViewModel>();
        builder.RegisterType<QualityProfileViewModel>().As<IWizardStepViewModel>();

        builder
            .Register(c => new CfGroupViewModel(
                c.Resolve<WizardViewModel>(),
                c.Resolve<CfGroupResourceQuery>(),
                CfGroupMode.SkipDefaults
            ))
            .As<IWizardStepViewModel>();

        builder
            .Register(c => new CfGroupViewModel(
                c.Resolve<WizardViewModel>(),
                c.Resolve<CfGroupResourceQuery>(),
                CfGroupMode.AddOptional
            ))
            .As<IWizardStepViewModel>();

        builder
            .Register(_ => new PlaceholderViewModel(
                "Quality Sizes",
                "Configure quality size limits."
            ))
            .As<IWizardStepViewModel>();

        builder
            .Register(_ => new PlaceholderViewModel(
                "Media Naming",
                "Set media file and folder naming conventions."
            ))
            .As<IWizardStepViewModel>();

        builder
            .Register(_ => new PlaceholderViewModel(
                "Review & Generate",
                "Review your configuration and generate YAML."
            ))
            .As<IWizardStepViewModel>();
    }
}
