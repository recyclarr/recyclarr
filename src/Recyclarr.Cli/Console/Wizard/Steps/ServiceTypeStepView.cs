using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using ReactiveUI;
using Recyclarr.Cli.Console.Wizard.ViewModels;
using Recyclarr.Cli.Console.Wizard.Widgets;
using Recyclarr.TrashGuide;
using Terminal.Gui.ViewBase;

namespace Recyclarr.Cli.Console.Wizard.Steps;

[SuppressMessage(
    "Reliability",
    "CA2000",
    Justification = "Terminal.Gui manages disposal via Add()"
)]
internal sealed class ServiceTypeStepView : WizardStepViewBase<ServiceTypeViewModel>
{
    public ServiceTypeStepView(ServiceTypeViewModel viewModel, WizardOptionSelector optionSelector)
    {
        ViewModel = viewModel;

        var question = CreateQuestion("What type of service are you configuring?");

        var selector = optionSelector.Create<SupportedServices>();
        selector.Y = Pos.Bottom(question) + 1;

        var hint = CreateHint(
            "Radarr is for movies. Sonarr is for TV series.",
            Pos.Bottom(selector) + 1
        );

        Add(question, selector, hint);

        // VM -> View: restore selection from ViewModel
        viewModel
            .WhenAnyValue(x => x.SelectedServiceType)
            .Subscribe(v => selector.Value = v)
            .DisposeWith(Disposables);

        // View -> VM
        selector
            .ObserveValue()
            .BindTo(viewModel, x => x.SelectedServiceType)
            .DisposeWith(Disposables);
    }
}
