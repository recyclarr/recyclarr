using Autofac;
using Recyclarr.Cli.Console.Wizard.ViewModels;
using Recyclarr.Cli.Console.Wizard.Widgets;

namespace Recyclarr.Cli.Console.Wizard;

internal sealed class WizardAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<WizardApplication>();
        builder.RegisterType<WizardViewModel>().SingleInstance();

        // Widget factories
        builder.RegisterType<WizardOptionSelector>();
        builder.RegisterType<WizardFlagSelector>();
        builder.RegisterType<WizardConfirmDialog>();

        // Step ViewModels registered in wizard flow order.
        // Autofac preserves registration order for IEnumerable<T>.
        builder.RegisterType<ServiceTypeViewModel>().As<IWizardStepViewModel>();
        builder.RegisterType<ConnectionViewModel>().As<IWizardStepViewModel>();
        builder.RegisterType<QualityProfileViewModel>().As<IWizardStepViewModel>();

        builder.RegisterType<CfGroupViewModel>().As<IWizardStepViewModel>();
        builder.RegisterType<QualitySizeViewModel>().As<IWizardStepViewModel>();
        builder.RegisterType<MediaNamingViewModel>().As<IWizardStepViewModel>();

        builder.RegisterType<ReviewViewModel>().As<IWizardStepViewModel>();
    }
}
