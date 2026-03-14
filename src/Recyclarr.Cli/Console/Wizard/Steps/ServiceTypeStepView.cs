using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using ReactiveUI;
using Recyclarr.Cli.Console.Wizard.ViewModels;
using Recyclarr.TrashGuide;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Recyclarr.Cli.Console.Wizard.Steps;

[SuppressMessage(
    "Reliability",
    "CA2000",
    Justification = "Terminal.Gui manages disposal via Add()"
)]
internal sealed class ServiceTypeStepView : WizardStepViewBase<ServiceTypeViewModel>
{
    public ServiceTypeStepView(ServiceTypeViewModel viewModel)
    {
        ViewModel = viewModel;

        var question = CreateQuestion("What type of service are you configuring?");

        var selector = new OptionSelector<SupportedServices> { Y = Pos.Bottom(question) + 1 };

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

        // View -> VM: manual event subscription because ReactiveMarbles
        // can't generate Events() for generic OptionSelector<TEnum>.ValueChanged
        Observable
            .FromEventPattern<
                EventHandler<EventArgs<SupportedServices?>>,
                EventArgs<SupportedServices?>
            >(h => selector.ValueChanged += h, h => selector.ValueChanged -= h)
            .Select(e => e.EventArgs.Value)
            .DistinctUntilChanged()
            .BindTo(viewModel, x => x.SelectedServiceType)
            .DisposeWith(Disposables);
    }
}
