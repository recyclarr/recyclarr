using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using Recyclarr.Cli.Console.Wizard.ViewModels;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Recyclarr.Cli.Console.Wizard.Steps;

[SuppressMessage(
    "Reliability",
    "CA2000",
    Justification = "Terminal.Gui manages disposal via Add()"
)]
[SuppressMessage(
    "Reliability",
    "CA2213",
    Justification = "Terminal.Gui manages disposal via Add()"
)]
internal sealed class QualityProfileStepView : WizardStepViewBase<QualityProfileViewModel>
{
    public QualityProfileStepView(QualityProfileViewModel viewModel)
    {
        ViewModel = viewModel;

        var question = CreateQuestion("Select one or more quality profiles");

        var selector = new FlagSelector { Y = Pos.Bottom(question) + 1 };

        var hint = CreateHint(
            "Profiles define quality preferences and scoring. Guide defaults will be used.\n"
                + "Press Enter with nothing selected to skip.",
            Pos.Bottom(selector) + 1
        );

        Add(question, selector, hint);

        // VM -> View: update selector labels and values when ViewModel recomputes
        viewModel
            .WhenAnyValue(x => x.Labels, x => x.Values)
            .Subscribe(tuple =>
            {
                var (labels, values) = tuple;
                selector.Labels = labels;
                selector.Values = values;
            })
            .DisposeWith(Disposables);

        // VM -> View: restore bitmask selection
        viewModel
            .WhenAnyValue(x => x.SelectedFlagValue)
            .Subscribe(v => selector.Value = v)
            .DisposeWith(Disposables);

        // View -> VM: propagate selection changes
        selector
            .Events()
            .ValueChanged.Select(e => e.NewValue)
            .DistinctUntilChanged()
            .BindTo(viewModel, x => x.SelectedFlagValue)
            .DisposeWith(Disposables);
    }
}
