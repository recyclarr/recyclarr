using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using ReactiveUI;
using Recyclarr.Cli.Console.Wizard.ViewModels;
using Recyclarr.Cli.Console.Wizard.Widgets;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Recyclarr.Cli.Console.Wizard.Steps;

internal enum QualitySizeChoice
{
    Yes,
    No,
}

[SuppressMessage(
    "Reliability",
    "CA2000",
    Justification = "Terminal.Gui manages disposal via Add()"
)]
internal sealed class QualitySizeStepView : WizardStepViewBase<QualitySizeViewModel>
{
    public QualitySizeStepView(QualitySizeViewModel viewModel, WizardOptionSelector optionSelector)
    {
        ViewModel = viewModel;

        var question = CreateQuestion("Sync quality sizes from TRaSH Guides?");

        var typePrefix = new Label
        {
            Text = "Quality definition type: ",
            Y = Pos.Bottom(question) + 1,
            SchemeName = WizardSchemes.HintText,
        };

        var typeValue = new Label
        {
            X = Pos.Right(typePrefix),
            Y = Pos.Bottom(question) + 1,
            SchemeName = WizardSchemes.ProgressCurrent,
        };

        var selector = optionSelector.Create<QualitySizeChoice>(Orientation.Horizontal);
        selector.Y = Pos.Bottom(typePrefix) + 1;

        var hint = CreateHint(
            "Sets file size limits per quality. Guide defaults are recommended.",
            Pos.Bottom(selector) + 1
        );

        Add(question, typePrefix, typeValue, selector, hint);

        // VM -> View: update type value when resolved type changes
        viewModel
            .WhenAnyValue(x => x.ResolvedType)
            .Subscribe(t => typeValue.Text = t)
            .DisposeWith(Disposables);

        // VM -> View: restore selection
        viewModel
            .WhenAnyValue(x => x.SelectedValue)
            .Select(v => v is true ? QualitySizeChoice.Yes : QualitySizeChoice.No)
            .Subscribe(v => selector.Value = v)
            .DisposeWith(Disposables);

        // View -> VM
        selector
            .ObserveValue()
            .Select(v => v is QualitySizeChoice.Yes)
            .BindTo(viewModel, x => x.SelectedValue)
            .DisposeWith(Disposables);
    }
}
